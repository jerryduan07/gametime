#!/usr/bin/env python

"""Exposes classes and functions to maintain a representation of,
and to interact with, an SMT solver.
"""

"""See the LICENSE file, located in the root directory of
the source distribution and
at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
for details on the GameTime license and authors.
"""


class Solver(object):
    """Maintains a representation of an SMT solver."""

    def __init__(self, name=""):
        """
        Constructor for the Solver class.

        @param name Name of the SMT solver that this object represents.
        """
        # Name of the SMT solver that this object represents.
        self.name = name

    def checkSat(self, query):
        """
        Checks and updates the satisfiability of the SMT query
        represented by the Query object provided. If the SMT query
        is satisfiable, the Query object is updated with a satisfying
        model; if the query is unsatisfiable, the Query object is
        updated with an unsatisfiable core.

        @param query Query object that represents an SMT query.
        """
        errMsg = "Method has not yet been implemented."
        raise NotImplementedError(errMsg)

    def __str__(self):
        """
        Returns a string representation of this Solver object.

        @retval String representation of this Solver object.
        """
        return self.name
