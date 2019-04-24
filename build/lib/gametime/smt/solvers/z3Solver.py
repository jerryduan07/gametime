#!/usr/bin/env python

"""Exposes classes and functions to maintain a representation of,
and to interact with, Z3, the SMT solver from Microsoft.
"""

"""See the LICENSE file, located in the root directory of
the source distribution and
at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
for details on the GameTime license and authors.
"""


import z3

from gametime.defaults import config
from gametime.gametimeError import GameTimeError
from gametime.smt.model import Model
from gametime.smt.solvers.solver import Solver


class Z3Solver(Solver):
    """
    This class maintains a representation of Z3,
    the SMT solver from Microsoft.
    """
    def __init__(self):
        """Constructor for the Z3Solver class."""
        super(Z3Solver, self).__init__("z3")

    def checkSat(self, query):
        """
        Checks and updates the satisfiability of the SMT query
        represented by the Query object provided. If the SMT query
        is satisfiable, the Query object is updated with a satisfying
        model; if the query is unsatisfiable, the Query object is
        updated with an unsatisfiable core.

        @param query Query object that represents an SMT query.
        """
        solver = z3.Solver()

        queryExpr = z3.parse_smt2_string(query.queryStr)
        if (not queryExpr.decl().kind() == z3.Z3_OP_AND or
            not queryExpr.children()[-1].decl().kind() == z3.Z3_OP_AND):
            errMsg = "SMT query is not in the form expected."
            raise GameTimeError(errMsg)

        # Assert all of the equivalences in the query.
        # (Ignore the last child of the `And' Boolean expression,
        # which is not an equivalence.)
        equivalences = queryExpr.children()[:-1]
        for equivalence in equivalences:
            solver.add(equivalence)

        # Obtain the Boolean variables associated with the constraints.
        constraintVars = [equivalence.children()[0] for equivalence
                          in equivalences]
        # Check the satisfiability of the query.
        querySatResult = solver.check(*constraintVars)
        if querySatResult == z3.sat:
            query.labelSat(Model(solver.model().sexpr()))
        elif querySatResult == z3.unsat:
            unsatCore = solver.unsat_core()
            unsatCore = [str(constraintVar) for constraintVar in unsatCore]
            unsatCore = [int(constraintNumStr[len(config.IDENT_CONSTRAINT):])
                         for constraintNumStr in unsatCore]
            query.labelUnsat(unsatCore)
        else:
            query.labelUnknown()

    def __str__(self):
        """
        Returns a string representation of this Z3Solver object.

        @retval String representation of this Z3Solver object.
        """
        return self.name
