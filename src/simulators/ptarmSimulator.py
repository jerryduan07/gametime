#!/usr/bin/env python

"""Exposes classes and functions to maintain a representation of,
and to interact with, the PTARM simulator, obtained from
http://chess.eecs.berkeley.edu/pret/src/ptarm-1.0/ptarm_simulator.html.
"""

"""See the LICENSE file, located in the root directory of
the source distribution and
at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
for details on the GameTime license and authors.
"""


import os
import re
import shutil
import subprocess
from tempfile import NamedTemporaryFile

from gametime.defaults import config
from gametime.fileHelper import createDir, removeFiles
from gametime.gametimeError import GameTimeError
from gametime.simulators.simulator import Simulator


class PtarmSimulator(Simulator):
    """Maintains a representation of the PTARM simulator."""

    def __init__(self, projectConfig):
        super(PtarmSimulator, self).__init__(projectConfig, "PTARM")

    def _createTestCaseFile(self, path):
        """Creates a temporary C file that contains the test case that
        would drive an execution of the code under analysis along
        the provided path.

        Arguments:
            path:
                :class:`~gametime.path.Path` object that represents
                the path along which the generated test case file
                should drive the execution of the code under analysis.

        Returns:
            Location of the temporary C file that contains the test case.
        """
        projectConfig = self.projectConfig

        contents = []
        contents.append("#include \"%s\"" % projectConfig.locationOrigFile)
        contents.append("")
        contents.append("int %s(void)" % config.ANNOTATION_SIMULATE)
        contents.append("{")
        for key in sorted(path.assignments.keys()):
            contents.append("  %s = %s;" % (key, path.assignments[key]))
        contents.append("  asm(\"start:\");")
        contents.append("  %s();" % projectConfig.func)
        contents.append("  asm(\"end:\");")
        contents.append("  return 0;")
        contents.append("}")
        contents.append("")

        testCaseFileHandler = NamedTemporaryFile(suffix="-gt.c",
                                                 dir=self._measurementDir,
                                                 delete=False)
        with testCaseFileHandler:
            testCaseFileHandler.write("\n".join(contents))
        return testCaseFileHandler.name

    def _compileFile(self, testCaseFileLocation):
        """Compiles the temporary file that contains a test case
        using the cross-compiler for the ARM target (arm-elf-gcc).

        Arguments:
            testCaseFileLocation:
                Location of the temporary file that contains the test case.

        Returns:
            Location of the binary output file produced by the compilation.
        """
        projectConfig = self.projectConfig

        outFileLocationNoExt, _ = os.path.splitext(testCaseFileLocation)
        outFileLocation = "%s.out" % outFileLocationNoExt

        compileCmd = []

        # This command prefix was suggested by
        # http://sharats.me/the-ever-useful-and-neat-subprocess-module.html.
        compileCmd.append("mintty")
        compileCmd.append("--hold")
        compileCmd.append("error")
        compileCmd.append("--exec")

        compileCmd.append("%s/bin/arm-elf-gcc" % config.SIMULATOR_TOOL_GNU_ARM)

        compileCmd.append("-I./")
        for includedFileLocation in projectConfig.included:
            compileCmd.append("-I'%s'" % includedFileLocation)
        compileCmd.append("-I'%s/include'" % config.SIMULATOR_TOOL_GNU_ARM)
        compileCmd.append("-I'%s/include'" % config.SIMULATOR_PTARM)
        compileCmd.append("-I'%s/tests/include'" % config.SIMULATOR_PTARM)

        compileCmd.append("-nostartfiles")
        compileCmd.append("-g")
        compileCmd.append("-mcpu=arm7di")
        compileCmd.append("-DSTACK_INIT=0x40100000")
        compileCmd.append("'%s/tests/crt/crt0.S'" % config.SIMULATOR_PTARM)
        compileCmd.append("-Ttext")
        compileCmd.append("0x40000000")
        compileCmd.append("-L'%s/lib'" % config.SIMULATOR_TOOL_GNU_ARM)

        compileCmd.append(testCaseFileLocation)

        compileCmd.append("-o")
        compileCmd.append(outFileLocation)

        returnCode = subprocess.call(compileCmd)
        if returnCode:
            errMsg = ("Error in compiling using the cross-compiler for "
                      "the ARM target (arm-elf-gcc).") 
            raise GameTimeError(errMsg)

        return outFileLocation

    def _dumpAsmFile(self, outFileLocation):
        """
        Arguments:
            outFileLocation:
                Location of the binary file produced by the compilation of
                a temporary file that contains a test case.

        Returns:
            Location of the file that contains the assembler contents
            of the binary file produced by the compilation of a temporary
            file that contains a test case.
        """ 
        asmFileLocationNoExt, _ = os.path.splitext(outFileLocation)
        asmFileLocation = "%s.asm" % asmFileLocationNoExt

        dumpCmd = []
        dumpCmd.append("%s/bin/arm-elf-objdump" %
                       config.SIMULATOR_TOOL_GNU_ARM)
        dumpCmd.append("-d")
        dumpCmd.append("-j")
        dumpCmd.append(".rodata")
        dumpCmd.append("-j")
        dumpCmd.append(".text")
        dumpCmd.append(outFileLocation)

        dumpCmdOutput = subprocess.check_output(dumpCmd)

        with open(asmFileLocation, "w") as asmFile:
            asmFile.write(dumpCmdOutput)

        return asmFileLocation

    def _convertToSrec(self, outFileLocation):
        """Converts the binary file produced by the compilation
        of a temporary file that contains a test case to
        the SREC format executed by the simulator.

        Arguments:
            outFileLocation:
                Location of the binary file produced by the compilation
                of a temporary file that contains a test case.
        """
        srecFileLocation = os.path.join(self._measurementDir, "thread0.srec")

        srecCmd = []
        srecCmd.append("%s/bin/arm-elf-objcopy" %
                       config.SIMULATOR_TOOL_GNU_ARM)
        srecCmd.append("--output-target")
        srecCmd.append("srec")
        srecCmd.append(outFileLocation)
        srecCmd.append(srecFileLocation)

        returnCode = subprocess.call(srecCmd)
        if returnCode:
            errMsg = ("Error in converting the binary file produced by "
                      "compilation to the SREC format executed by "
                      "the simulator (arm-elf-objcopy).") 
            raise GameTimeError(errMsg)

    def _copyEvecSrec(self):
        """Copies an SREC file, which contains an exception vector table
        that is used by the simulator, to the temporary directory used
        for simulation.
        """
        EVEC_SREC_LOCATION = "%s/tests/crt/evec.srec" % config.SIMULATOR_PTARM
        evecSrecCopyLocation = os.path.join(self._measurementDir, "evec.srec")
        shutil.copy(EVEC_SREC_LOCATION, evecSrecCopyLocation)

    def _runSimulatorAndParseOutput(self, asmFileLocation):
        """Runs the simulator on a test case and dumps the output to
        a temporary file in the temporary directory used for simulation.
        This method then parses this output to determine the cycle count
        of the simulation.

        Arguments:
            asmFileLocation:
                Location of the file that contains the assembler contents
                of the binary file produced by the compilation of a temporary
                file that contains the test case.

        Returns:
            Cycle count of a simulation of the test case.
        """
        # Get the start/end address based on labels in the generated ASM file.
        with open(asmFileLocation, "r") as asmFile:
            asmLines = asmFile.read()
            match = re.search("([0-9a-f]+) <start>:", asmLines)
            startAddress = match.group(1)
            match = re.search("([0-9a-f]+) <end>:", asmLines)
            endAddress = match.group(1)

        # Construct the location of the PRET binary file.
        PRET_LOCATION = "%s/bin/pret" % config.SIMULATOR_PTARM

        # Run the PRET binary file and store the output.
        pretExecCmd = []
        pretExecCmd.append(PRET_LOCATION)
        pretExecCmd.append(self._measurementDir)
        pretExecCmd.append("-d")
        pretExecCmd.append("wfD1")

        pretOutput = subprocess.check_output(pretExecCmd)

        # Write the output to a file for later perusal.
        pretOutputFileLocationNoExt, _ = os.path.splitext(asmFileLocation)
        pretOutputFileLocation = "%s.pret.out" % pretOutputFileLocationNoExt
        with open(pretOutputFileLocation, "w") as pretOutputFile:
            pretOutputFile.write("".join(pretOutput))

        # Parse the output data for the start and end cycle counts.
        # Find the start/end address and extract the start/end index.
        addressString = (r"T0\|FE\|([0-9]+)> Fetched from PC: 0x{} "
                          "Binary: 0x[0-9a-f]+")
        startString = addressString.format(startAddress)
        match = re.search(startString, pretOutput)
        startIndex = match.group(1)
        endString = addressString.format(endAddress)
        match = re.search(endString, pretOutput)
        endIndex = match.group(1)

        # Use the start/end index to extract the start/end cycle count.
        cycleString = r"T0\|WB\|{}> Thread virtual cycle count: ([0-9]+)"
        startString = cycleString.format(startIndex)
        match = re.search(startString, pretOutput)
        startCycle = int(match.group(1))
        endString = cycleString.format(endIndex)
        match = re.search(endString, pretOutput)
        endCycle = int(match.group(1))

        totalCycles = endCycle - startCycle
        return totalCycles

    def _removeTemps(self):
        """Removes the temporary files and directory that were created
        during the most recent simulation, if any.
        """
        removeFiles([".*"], self._measurementDir)
        os.removedirs(self._measurementDir)

    def measure(self, path):
        """
        Arguments:
            path:
                :class:`~gametime.path.Path` object that represents the path
                whose cycle count needs to be measured.

        Returns:
            Cycle count of the path, as measured on the PTARM simulator.
        """
        # Create the temporary directory where the temporary files generated
        # during measurement will be stored.
        createDir(self._measurementDir)

        try:
            # This sequence of function calls closely mimics the steps
            # described at
            # http://chess.eecs.berkeley.edu/pret/src/ptarm-1.0/\
            # ptarm_simulator.html#%5B%5BCompiling%20Programs%5D%5D.
            testCaseFileLocation = self._createTestCaseFile(path)
            outFileLocation = self._compileFile(testCaseFileLocation)
            asmFileLocation = self._dumpAsmFile(outFileLocation)
            self._convertToSrec(outFileLocation)
            self._copyEvecSrec()
            cycleCount = self._runSimulatorAndParseOutput(asmFileLocation)
        except EnvironmentError as e:
            errMsg = ("Error in measuring the cycle count of a path "
                      "when simulated on the PTARM simulator: %s" % e)
            raise GameTimeError(errMsg)

        if not self.projectConfig.debugConfig.KEEP_SIMULATOR_OUTPUT:
            self._removeTemps()
        return cycleCount
