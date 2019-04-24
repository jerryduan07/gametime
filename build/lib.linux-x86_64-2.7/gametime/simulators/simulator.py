#!/usr/bin/env python

"""Exposes classes and functions to maintain a representation of, and
to interact with, a simulator, which will be used to measure values
that correspond to different paths in the code that is being analyzed.
"""

"""See the LICENSE file, located in the root directory of
the source distribution and
at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
for details on the GameTime license and authors.
"""


import os

from gametime.defaults import config, logger
from gametime.gametimeError import GameTimeError


class Simulator(object):
    """Maintains a representation of a simulator, which will be used
    to measure values that correspond to different paths in the code
    that is being analyzed.

    Attributes:
        projectConfig:
            GameTime project configuration for the code that
            is being analyzed.
        name:
            Name of the simulator that this object represents.
    """

    def __init__(self, projectConfig, name=""):
        #: Name of the simulator that this object represents.
        self.name = name

        #: GameTime project configuration for the code that
        #: is being analyzed.
        self.projectConfig = projectConfig

        #: Path to the temporary directory that will store
        #: the temporary files that are generated during measurement.
        self._measurementDir = os.path.join(projectConfig.locationTempDir,
                                            "%s-%s" % (config.TEMP_MEASUREMENT,
                                                       self.name.lower()))

    def measure(self, path):
        """
        Arguments:
            path:
                :class:`~gametime.path.Path` object that represents the path
                whose value needs to be measured.

        Returns:
            Value of the path, as measured on the simulator
            that is represented by this object.
        """
        return 0

    def measurePaths(self, paths):
        """
        Arguments:
            paths:
                List of paths whose values need to be measured, each
                represented by a :class:`~gametime.path.Path` object.

        Returns:
            List of the values of the paths in the list provided, as
            measured on the simulator that is represented by this object.
        """
        measurements = []
        for pathNum, path in enumerate(paths):
            logger.info("Measuring the value of path %d..." % (pathNum+1))

            measurement = self.measure(path)
            path.measuredValue = measurement
            measurements.append(measurement)

            logger.info("Value measured.")
            logger.info("")
        return measurements

    @staticmethod
    def readMeasurementsFromFile(location):
        """Reads, from a file, the measurements of feasible paths.

        Arguments:
            location:
                Location of the file that contains measurements
                of feasible paths.

        Returns:
            List of the measurements of feasible paths.
        """
        try:
            measurementsFileHandler = open(location, "r")
        except EnvironmentError as e:
            errMsg = ("Error reading the measurements from the file located "
                      "at %s: %s" % (location, e))
            raise GameTimeError(errMsg)
        else:
            with measurementsFileHandler:
                measurements = []
                measurementLines = measurementsFileHandler.readlines()
                for measurementLine in measurementLines:
                    _, measurement = measurementLine.strip().split()
                    measurements.append(float(measurement))
                return measurements

    def writeMeasurementsToFile(self, location, paths):
        """Measures the values of the paths in the list provided, and
        records these values in the file whose location is provided.
        The format of the file is the same as that expected by
        the ``loadBasisValuesFromFile`` method of the ``Analyzer`` class.

        Arguments:
            location:
                Location of the file where the values will be recorded.
            paths:
                List of paths whose values need to be measured, each
                represented by a :class:`~gametime.path.Path` object.
        """
        logger.info("Measuring the values of paths on the%ssimulator..." %
                    (" " if self.name == "" else (" %s " % self.name)))

        try:
            measurementsFileHandler = open(location, "w")
        except EnvironmentError as e:
            errMsg = ("Error writing the path values to the file located "
                      "at %s: %s" % (location, e))
            raise GameTimeError(errMsg)
        else:
            with measurementsFileHandler:
                measurements = self.measurePaths(paths)
                for pathNum, value in enumerate(measurements):
                    measurementsFileHandler.write("%d\t%d\n" %
                                                  ((pathNum + 1), value))

            logger.info("Measurement of all path values complete.")
