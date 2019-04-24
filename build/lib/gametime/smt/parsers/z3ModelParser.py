#!/usr/bin/env python

"""Exposes classes and functions to parse the models generated by Z3,
the SMT solver from Microsoft, in response to satisfiable SMT queries.
"""

"""See the LICENSE file, located in the root directory of
the source distribution and
at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
for details on the GameTime license and authors.
"""


from gametime.defaults import config
from gametime.smt.parsers.modelParser import Mapping, ModelParser
from gametime.smt.parsers.z3ModelLexer import Z3ModelLexer


class Z3Function(Mapping):
    """
    This class stores information about a function, as described
    by a model produced by Z3 in response to a satisfiable SMT query.
    """
    def __init__(self, modelParser, name="", defaultOutput=None):
        """
        Constructor for the Z3Function class.

        @param modelParser ModelParser object that contains
        this Z3Function object.
        @param name Name of the function.
        @param defaultOutput Default output of the function.
        """
        super(Z3Function, self).__init__(modelParser, name, defaultOutput)

    def get(self, inputVal, projectConfig):
        """
        Gets the output value that the input value provided is mapped to.

        @param inputVal Input value.
        @param projectConfig ProjectConfiguration object.
        @retval Output value that the input value is mapped to.
        """
        return self.outputs.get(inputVal,
                                self.defaultOutput.get(inputVal,
                                                       projectConfig)
                                if self.defaultOutput is not None else 0)


class Z3ConstantFunction(Z3Function):
    """
    This class stores information about a constant function, as described
    by a model produced by Z3 in response to a satisfiable SMT query.
    """
    def __init__(self, modelParser, name="", defaultOutput=0):
        """
        Constructor for the Z3ConstantFunction class.

        @param modelParser ModelParser object that contains
        this Z3ConstantFunction object.
        @param name Name of the function.
        @param defaultOutput Default output of the function.
        """
        super(Z3ConstantFunction, self).__init__(modelParser, name,
                                                 defaultOutput)

    def get(self, inputVal, projectConfig):
        """
        Gets the output value that the input value provided is mapped to.

        @param inputVal Input value.
        @param projectConfig ProjectConfiguration object.
        @retval Output value that the input value is mapped to.
        """
        return self.defaultOutput


class Z3Array(Mapping):
    """
    This class stores information about an array, as described
    by a model produced by Z3 in response to a satisfiable SMT query.
    """
    def __init__(self, modelParser, name="", functionName=""):
        """
        Constructor for the Z3Array class.

        @param modelParser ModelParser object that contains
        this Z3Array object.
        @param name Name of the array.
        @param functionName Name of the Z3 function that is
        interpreted as the array.
        """
        super(Z3Array, self).__init__(modelParser, name)

        # Name of the Z3 function that is interpreted as the array.
        self.functionName = functionName

    def get(self, indices, projectConfig):
        """
        Gets the value of the array at the indices specified.

        @param indices Tuple of indices accessed in the array.
        For example, if this Z3Array object represents a two-dimensional
        array `a', the value of `a[0][1]' can be obtained by calling
        this function with the tuple (0, 1).
        @param projectConfig ProjectConfiguration object.
        @retval Value of the array at the indices specified.
        """
        if not projectConfig.MODEL_AS_NESTED_ARRAYS:
            indexSize = config.WORD_BITSIZE / 4
            hexIndices = ""
            for index in indices:
                hexIndex = ModelParser.decToHex(index)[2:]
                hexIndices += hexIndex.zfill(indexSize)
            indices = (ModelParser.hexToDec(hexIndices),)

        z3Function = self.modelParser.allMappings[self.functionName]
        if len(indices) == 1:
            return z3Function.get(indices[0], projectConfig)
        else:
            nestedZ3Function = z3Function.get(indices[0], projectConfig)
            return nestedZ3Function.get(indices[1:], projectConfig)


class Z3ModelParser(ModelParser):
    """
    This class parses the models generated by Z3,
    the SMT solver from Microsoft.
    """
    def __init__(self):
        """
        Constructor for the Z3ModelParser class.

        @param projectConfig ProjectConfiguration object.
        """
        super(Z3ModelParser, self).__init__()

        # Z3ModelLexer object that performs the lexical analysis
        # of the models generated by Z3.   
        self.modelLexer = Z3ModelLexer()

    ### PARSER GRAMMAR RULES ###
    def p_model(self, p):
        """model : assign
                 | assign model"""
        p[0] = None

    def p_type_boolean(self, p):
        """type_boolean : BOOL"""
        p[0] = None

    def p_type_bv(self, p):
        """type_bv : LPAREN WORD BITVEC NUMBER RPAREN"""
        p[0] = None

    def p_type_array(self, p):
        """type_array : LPAREN ARRAY type_bv type_bv RPAREN
                      | LPAREN ARRAY type_bv type_array RPAREN"""
        p[0] = None

    def p_var_name(self, p):
        """var_name : WORD"""
        p[0] = p[1]

    # Grammar rule for variables whose name is a special token.
    def p_var_name_special(self, p):
        """var_name : BOOL
                    | BITVEC
                    | ARRAY
                    | BOOL var_name
                    | BITVEC var_name
                    | ARRAY var_name
                    | WORD var_name"""
        p[0] = p[1]

    def p_temp_var(self, p):
        """temp_var : var_name LANGLE NUMBER RANGLE"""
        p[0] = p[1] + p[2] + str(p[3]) + p[4]

    def p_index_var(self, p):
        """index_var : TEMPINDEX NUMBER"""
        p[0] = p[2]

    def p_temp_z3_var(self, p):
        """temp_z3_var : WORD BANG NUMBER"""
        p[0] = p[1] + p[2] + str(p[3])

    def p_efc_var(self, p):
        """efc_var : EFC var_name AT NUMBER"""
        p[0] = p[1] + p[2] + p[3] + str(p[4])

    def p_constraint_var(self, p):
        """constraint_var : CONSTRAINT NUMBER"""
        p[0] = None

    def p_func_no_args(self, p):
        """func_no_args : LPAREN RPAREN"""
        p[0] = None

    def p_func_bv_arg(self, p):
        """func_bv_arg : LPAREN LPAREN temp_z3_var type_bv RPAREN RPAREN"""
        p[0] = None

    def p_define_var(self, p):
        """define_var : DEFINEFUN var_name func_no_args type_bv"""
        p[0] = p[2]

    def p_define_temp_var(self, p):
        """define_temp_var : DEFINEFUN temp_var func_no_args type_bv"""
        p[0] = p[2]

    def p_define_index_var(self, p):
        """define_index_var : DEFINEFUN index_var func_no_args type_bv"""
        p[0] = p[2]

    def p_define_array_var(self, p):
        """define_array_var : DEFINEFUN var_name func_no_args type_array
                            | DEFINEFUN temp_var func_no_args type_array"""
        p[0] = p[2]

    def p_define_z3_func_var(self, p):
        """define_z3_func_var : DEFINEFUN temp_z3_var func_bv_arg type_bv
                              | DEFINEFUN temp_z3_var func_bv_arg type_array"""
        p[0] = p[2]

    def p_define_efc_var(self, p):
        """define_efc_var : DEFINEFUN efc_var func_no_args type_bv"""
        p[0] = p[2]

    def p_define_constraint_var(self, p):
        """define_constraint_var : DEFINEFUN constraint_var func_no_args \
                                   type_boolean"""
        p[0] = p[2]

    def p_assign_var(self, p):
        """assign : LPAREN define_var HEXNUMBER RPAREN
                  | LPAREN define_var BINNUMBER RPAREN"""
        self.allAssignments[p[2]] = p[3]
        p[0] = None

    def p_assign_temp_var(self, p):
        """assign : LPAREN define_temp_var HEXNUMBER RPAREN
                  | LPAREN define_temp_var BINNUMBER RPAREN"""
        self.allAssignments[p[2]] = p[3]
        p[0] = None

    def p_assign_index_var(self, p):
        """assign : LPAREN define_index_var HEXNUMBER RPAREN"""
        self.arrayTempIndexVals[p[2]] = p[3]
        p[0] = None

    def p_z3_func_as_array(self, p):
        """z3_func_as_array : LPAREN WORD ASARRAY temp_z3_var RPAREN"""
        p[0] = Z3Array(self, functionName=p[4])

    def p_assign_array_var(self, p):
        """assign : LPAREN define_array_var z3_func_as_array RPAREN"""
        p[3].name = p[2]
        self.allMappings[p[2]] = p[3]
        p[0] = None

    def p_assign_z3_const_func_var(self, p):
        """assign : LPAREN define_z3_func_var HEXNUMBER RPAREN
                  | LPAREN define_z3_func_var BINNUMBER RPAREN
                  | LPAREN define_z3_func_var z3_func_as_array RPAREN"""
        self.allMappings[p[2]] = Z3ConstantFunction(self, p[2], p[3])
        p[0] = None

    def p_assign_z3_func_var(self, p):
        """assign : LPAREN define_z3_func_var func_ite RPAREN"""
        p[3].name = p[2]
        self.allMappings[p[2]] = p[3]
        p[0] = None

    def p_func_ite_condition(self, p):
        """ite_condition : LPAREN EQUALS temp_z3_var HEXNUMBER RPAREN"""
        p[0] = p[4]

    def p_func_ite_else(self, p):
        """func_ite : LPAREN WORD ite_condition HEXNUMBER HEXNUMBER RPAREN
                    | LPAREN WORD ite_condition BINNUMBER BINNUMBER RPAREN
                    | LPAREN WORD ite_condition z3_func_as_array \
                      z3_func_as_array RPAREN"""
        z3ConstantFunction = Z3ConstantFunction(self, defaultOutput=p[5])
        z3Function = Z3Function(self, defaultOutput=z3ConstantFunction)
        z3Function.add(p[3], p[4])
        p[0] = z3Function

    def p_func_ite(self, p):
        """func_ite : LPAREN WORD ite_condition HEXNUMBER func_ite RPAREN
                    | LPAREN WORD ite_condition BINNUMBER func_ite RPAREN
                    | LPAREN WORD ite_condition z3_func_as_array \
                      func_ite RPAREN"""
        p[5].add(p[3], p[4])
        p[0] = p[5]

    def p_assign_efc_var(self, p):
        """assign : LPAREN define_efc_var HEXNUMBER RPAREN
                  | LPAREN define_efc_var BINNUMBER RPAREN"""
        self.allAssignments[p[2]] = p[3]
        p[0] = None

    def p_assign_constraint_var(self, p):
        """assign : LPAREN define_constraint_var TRUE RPAREN"""
        p[0] = None