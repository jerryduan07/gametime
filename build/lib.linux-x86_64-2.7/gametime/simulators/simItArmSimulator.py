#!/usr/bin/env python

"""Exposes classes and functions to maintain a representation of,
and to interact with, the SimIt-ARM simulator v2.1.
"""

"""See the LICENSE file, located in the root directory of
the source distribution and
at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
for details on the GameTime license and authors.
"""


import os
from tempfile import NamedTemporaryFile

from fabric.api import cd, env, execute, put, run

from gametime.defaults import config
from gametime.fileHelper import createDir
from gametime.gametimeError import GameTimeError
from gametime.simulators.simulator import Simulator


class SimItArmSimulator(Simulator):
    """Maintains a representation of the SimIt-ARM simulator."""

    def __init__(self, projectConfig):
        super(SimItArmSimulator, self).__init__(projectConfig, "SimIt-ARM")

        #: Path to the temporary directory on the remote machine that will
        #: store the temporary files that are generated during measurement.
        dirName = "%s%s" % (projectConfig.nameOrigNoExtension,
                            config.TEMP_SUFFIX)
        self._remoteMeasurementDir = os.path.join("/tmp/%s" % dirName)

    def _getRemotePath(self, location):
        """
        Arguments:
            location:
                Location of a file on the local machine.

        Returns:
            Corresponding location of the file on the remote machine.
            If transferred to this location, the file will be stored
            in the temporary directory on the remote machine created
            for the purposes of simulation and measurement.
        """
        return "%s/%s" % (self._remoteMeasurementDir,
                          os.path.basename(location))

    def _transferFile(self, location):
        """
        Transfers the file at the location provided into the temporary
        directory on the remote machine that stores the temporary files
        generated during simulation and measurement.

        Arguments:
            location:
                Location of a file on the local machine.
        """
        result = put(location,
                     self._getRemotePath(location),
                     mirror_local_mode=True)
        if len(result.failed) > 0:
            errMsg = ("Error in uploading the file located at %s to "
                      "a remote machine: %s" % (location, e))
            raise GameTimeError(errMsg)

    def _createTestCaseFile(self, path, addFunctionCall=True):
        """Creates a temporary C file that contains the test case that
        would drive an execution of the code under analysis along
        the provided path.

        Arguments:
            path:
                :class:`~gametime.path.Path` object that represents
                the path along which the generated test case file
                should drive the execution of the code under analysis.
            addFunctionCall:
                True if, and only if, the test case should contain a call
                to the function that contains the code under analysis.

        Returns:
            Location of the temporary C file that contains the test case.
        """
        projectConfig = self.projectConfig

        contents = []
        contents.append("#include \"%s\"" % projectConfig.nameOrigFile)
        contents.append("")
        contents.append("int %s(void)" % config.ANNOTATION_SIMULATE)
        contents.append("{")
        for key in sorted(path.assignments.keys()):
            contents.append("  %s = %s;" % (key, path.assignments[key]))
        if addFunctionCall:
            contents.append("  %s();" % projectConfig.func)
        contents.append("  return 0;")
        contents.append("}")
        contents.append("")

        tempFilePrefix = "%s%s-" % (config.TEMP_CASE,
                                    ("" if addFunctionCall else "-0"))
        testCaseFileHandler = NamedTemporaryFile(prefix=tempFilePrefix,
                                                 suffix="-gt.c",
                                                 dir=self._measurementDir,
                                                 delete=False)
        with testCaseFileHandler:
            testCaseFileHandler.write("\n".join(contents))
        return testCaseFileHandler.name

    def _createZeroTestCaseFile(self, path):
        """Creates a temporary C file that contains the "zero" test case,
        or the test case that does not call the function that contains
        the code under analysis.

        Arguments:
            path:
                :class:`~gametime.path.Path` object that represents
                a feasible path in the function.

        Returns:
            Location of the temporary C file that contains
            the "zero" test case.
        """
        return self._createTestCaseFile(path, addFunctionCall=False)

    def _isZeroTestCaseFile(self, location):
        """
        Arguments:
            location:
                Location of a file.

        Returns:
            True if, and only if, the file at the location provided
            contains a "zero" test case, or a test case that does not
            call the function that contains the code under analysis.
        """
        return os.path.basename(location).startswith("%s-0" % config.TEMP_CASE)

    def measure(self, path):
        """
        Arguments:
            path:
                :class:`~gametime.path.Path` object that represents the path
                whose cycle count needs to be measured.

        Returns:
            Cycle count of the path, as measured on the SimIt-ARM simulator.
        """
        # Create the temporary directory where the temporary files generated
        # during measurement will be stored.
        createDir(self._measurementDir)

        try:
            if run("mkdir -p %s" % self._remoteMeasurementDir):
                errMsg = ("Error in creating the temporary directory "
                          "for measurements on the remote computer.")
                raise GameTimeError(errMsg)

            testCaseFileLocation = self._createTestCaseFile(path)
            self._transferFile(testCaseFileLocation)
            zeroTestCaseFileLocation = self._createZeroTestCaseFile(path)
            self._transferFile(zeroTestCaseFileLocation)

            if run("rm -rf %s" % self._remoteMeasurementDir):
                errMsg = ("Error in removing the temporary directory "
                          "for measurements on the remote computer.")
                raise GameTimeError(errMsg)
        except EnvironmentError as e:
            errMsg = ("Error in measuring the cycle count of a path "
                      "when simulated on the SimIt-ARM simulator: %s" % e)
            raise GameTimeError(errMsg)

        # TODO (jokotker): Not completely integrated.
        return 0


if __name__ == "__main__":
    env.hosts = ["uclid.eecs.berkeley.edu"]
    env.user = "jkotker"

    # from gametime.projectConfiguration import readProjectConfigFile
    # projectConfigXmlFile = "C:\\Research\\GameTime-v1\\demo\\modexp_unrolled\\projectConfig.xml"
    # projectConfig = readProjectConfigFile(projectConfigXmlFile)

    # from gametime import GameTime
    # analyzer = GameTime.analyze(projectConfig)
    # basisPaths = analyzer.generateBasisPaths()

    from gametime import Analyzer
    analyzer = Analyzer.loadFromFile("C:\\Research\\GameTime-v1\\demo\\modexp_unrolled\\analyzer")
    basisPaths = analyzer.basisPaths
    simulator = SimItArmSimulator(analyzer.projectConfig)

    from functools import partial
    execute(partial(simulator.measure, basisPaths[0]))
    execute(partial(simulator.measure, basisPaths[1]))
