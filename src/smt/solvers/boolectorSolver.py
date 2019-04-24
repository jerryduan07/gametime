#!/usr/bin/env python

"""Exposes classes and functions to maintain a representation of,
and to interact with, the Boolector SMT solver.
"""

"""See the LICENSE file, located in the root directory of
the source distribution and
at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
for details on the GameTime license and authors.
"""


import subprocess
from tempfile import NamedTemporaryFile

from gametime.defaults import config
from gametime.gametimeError import GameTimeError
from gametime.smt.model import Model
from gametime.smt.solvers.solver import Solver


class SatSolver(object):
    """This class represents the SAT solver used by Boolector."""
    # Lingeling SAT solver (default).
    LINGELING = 0
    # MiniSat SAT solver.
    MINISAT = 1
    # PicoSAT SAT solver.
    PICOSAT = 2

    @staticmethod
    def getSatSolver(satSolverName):
        """
        Returns the SatSolver representation of the SAT solver
        whose name is provided.

        @param satSolverName Name of a SAT solver.
        @retval SatSolver representation of the SAT solver
        whose name is provided.
        """
        satSolverName = satSolverName.lower()
        if satSolverName == "lingeling" or satSolverName == "":
            return SatSolver.LINGELING
        elif satSolverName == "minisat":
            return SatSolver.MINISAT
        elif satSolverName == "picosat":
            return SatSolver.PICOSAT
        else:
            errMsg = ("Unknown backend SAT solver for Boolector: %s" %
                      satSolverName)
            raise GameTimeError(errMsg)

    @staticmethod
    def getName(satSolver):
        """
        Returns the name of the SAT solver whose SatSolver representation
        is provided.

        @param satSolver SatSolver representation of a SAT solver.
        @retval Name of the SAT solver whose SatSolver representation
        is provided.
        """
        if satSolver == SatSolver.LINGELING:
            return "lingeling"
        elif satSolver == SatSolver.MINISAT:
            return "minisat"
        elif satSolver == SatSolver.PICOSAT:
            return "picosat"
        else:
            errMsg = ("Unknown backend SAT solver for Boolector: %s" %
                      satSolver)
            raise GameTimeError(errMsg)


class BoolectorSolver(Solver):
    """This class maintains a representation of the Boolector SMT solver."""
    def __init__(self, satSolver=SatSolver.LINGELING):
        """
        Constructor for the BoolectorSolver class.

        @param satSolver SatSolver representation of the SAT solver that
        the Boolector executable uses.
        """
        super(BoolectorSolver, self).__init__("boolector")

        # SatSolver representation of the SAT solver that
        # the Boolector executable uses.
        self.satSolver = satSolver

    def _generateBoolectorCommand(self, location):
        """
        Generates the system call to run Boolector on a file containing
        an SMT query whose location is provided.

        @param location Location of a file containing an SMT query.
        @retval Appropriate system call as a list that contains the program
        to be run and the proper arguments.
        """
        command = []

        command.append(config.SOLVER_BOOLECTOR)
        command.append("--model")
        command.append("--smt2")
        command.append("-" + SatSolver.getName(self.satSolver))
        command.append(location)

        return command

    def checkSat(self, query):
        """
        Checks and updates the satisfiability of the SMT query
        represented by the Query object provided. If the SMT query
        is satisfiable, the Query object is updated with a satisfying
        model; if the query is unsatisfiable, the Query object is
        updated with an unsatisfiable core.

        @param query Query object that represents an SMT query.
        """
        # Write the SMT query to a temporary file.
        smtQueryFileHandler = NamedTemporaryFile()
        with smtQueryFileHandler:
            smtQueryFileHandler.write(query.queryStr)
            smtQueryFileHandler.seek(0)

            command = self._generateBoolectorCommand(smtQueryFileHandler.name)
            proc = subprocess.Popen(command,
                                    stdout=subprocess.PIPE,
                                    stderr=subprocess.PIPE,
                                    shell=True)
            output = proc.communicate()[0]
            outputLines = output.strip().split("\n")

            isSat = outputLines.index("sat") if "sat" in outputLines else None
            isUnsat = "unsat" in outputLines
            if isSat is not None:
                query.labelSat(Model("\n".join(outputLines[isSat+1:])))
            elif isUnsat:
                query.labelUnsat([])
            else:
                query.labelUnknown()

    def __str__(self):
        """
        Returns a string representation of this BoolectorSolver object.

        @retval String representation of this BoolectorSolver object.
        """
        return self.name + "-" + SatSolver.getName(self.satSolver)
