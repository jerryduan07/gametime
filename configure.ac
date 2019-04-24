AC_INIT([GameTime], [1.5], [sseshia@eecs.berkeley.edu], [gametime],
[http://uclid.eecs.berkeley.edu/gametime])

AM_INIT_AUTOMAKE

# For configuration of the Python module files.
AM_PATH_PYTHON([2.7])

# jkotker: There appears to be a bug in aclocal, whereby the directories
# that are used by Python to install modules and packages (both
# platform-independent and third-party) are inferred incorrectly.
# The following attempts to correctly infer this directory, but it
# only works for a Windows-based Python installation.

# Check if the user has supplied a prefix to "configure".
if test "x$prefix" = "xNONE"; then
    PREFIX_ARG=None
else
    PREFIX_ARG=\"$prefix\"
fi

PYTH_PKGDIR=[`$PYTHON -c \
"import sys; from distutils.sysconfig import get_python_lib; \
sys.stdout.write(get_python_lib(0, 0, $PREFIX_ARG))" 2>/dev/null`]
PYTH_PKGDIR_UNIX=[`cygpath -u "$PYTH_PKGDIR"`]

PYTH_PREFIX=[`$PYTHON -c \
"import sys; sys.stdout.write(sys.prefix)" 2>/dev/null`]
PYTH_PREFIX_UNIX=[`cygpath -u "$PYTH_PREFIX"`]

PYTH_SCRIPTSDIR_UNIX=[$PYTH_PREFIX_UNIX/Scripts]
PYTH_SCRIPTSDIR=[`cygpath -w "$PYTH_SCRIPTSDIR_UNIX"`]

# Windows path equivalent of the $srcdir variable.
SRCDIR_WIN=[`cygpath -w "$srcdir"`]

# For the Makefiles of the Phoenix program analysis code.
if test "x$WINDIR" = "x"; then
    WINDIR_UNIX=""
else
    WINDIR_UNIX=[`cygpath -u "$WINDIR"`]
fi

if test "x`env | grep ^"ProgramFiles(x86)"`" != "x"; then
    PROGRAM_FILES=[`env | grep ^"ProgramFiles(x86)" | sed -e s/^.*=//g`]
elif test "x`env | grep ^PROGRAMFILES`" != "x"; then
    PROGRAM_FILES=$PROGRAMFILES
else
    PROGRAM_FILES=""
fi

# For configuration of the CIL files and
# the Makefile rules that interact with the CIL files.
ARCHOS=x86_WIN32
GAMETIME_SRCDIR_UNIX=\
[$PYTH_PKGDIR_UNIX/$PACKAGE-$VERSION-py$PYTHON_VERSION.egg/$PACKAGE]
GAMETIME_SRCDIR=[`cygpath -w "$GAMETIME_SRCDIR_UNIX"`]
GAMETIME_SRCDIR_MIXED=[`cygpath -m "$GAMETIME_SRCDIR_UNIX"`]
CILHOME_UNIX=[$GAMETIME_SRCDIR_UNIX/cil]

# For configuration of GameTime.
# Find the location of the Cygwin home directory.
CYGWINDIR=[`cygpath -w /`]

# Guess the location of the Boolector executable.
BOOLECTOR_EXE_UNIX=[`if command -v boolector.exe > /dev/null 2>&1; \
then which boolector.exe; fi`]
if test "x$BOOLECTOR_EXE_UNIX" = "x"; then
    BOOLECTOR_EXE=""
else
    BOOLECTOR_EXE=[`cygpath -w "$BOOLECTOR_EXE_UNIX"`]
fi

# Guess the location of the Python interface of Z3,
# the SMT solver from Microsoft.
Z3_PYC_UNIX=[`if command -v z3.pyc > /dev/null 2>&1; then which z3.pyc; fi`]
if test "x$Z3_PYC_UNIX" = "x"; then
    Z3_PYC=""
else
    Z3_PYC=[`cygpath -w "$Z3_PYC_UNIX"`]
fi

# Guess the location of the directory that contains the GNU-ARM toolchain.
if test "x$ARM_ELF_GCC" != "x"; then
    GNU_ARM_UNIX=$ARM_ELF_GCC
else
    GNU_ARM_UNIX=[`if command -v arm-elf-gcc.exe > /dev/null 2>&1; \
    then which arm-elf-gcc.exe; fi`]
fi

if test "x$GNU_ARM_UNIX" = "x"; then
    GNU_ARM=""
else
    # Find the directory two levels higher in the hierarchy.
    GNU_ARM_UNIX=[`dirname "$(dirname "$GNU_ARM_UNIX")"`]
    GNU_ARM=[`cygpath -w "$GNU_ARM_UNIX"`]
fi

# Guess the location of the directory that contains the PTARM simulator.
PTARM_UNIX=[`if command -v pret > /dev/null 2>&1; then which pret; fi`]
if test "x$PTARM_UNIX" = "x"; then
    PTARM=""
else
    # Find the directory two levels higher in the hierarchy.
    PTARM_UNIX=[`dirname "$(dirname "$PTARM_UNIX")"`]
    PTARM=[`cygpath -w "$PTARM_UNIX"`]
fi

AC_SUBST([ARCHOS])
AC_SUBST([BOOLECTOR_EXE])
AC_SUBST([CC])
AC_SUBST([CILHOME_UNIX])
AC_SUBST([CYGWINDIR])
AC_SUBST([GAMETIME_SRCDIR])
AC_SUBST([GAMETIME_SRCDIR_MIXED])
AC_SUBST([GAMETIME_SRCDIR_UNIX])
AC_SUBST([GNU_ARM])
AC_SUBST([PROGRAM_FILES])
AC_SUBST([PTARM])
AC_SUBST([PYTH_PREFIX_UNIX])
AC_SUBST([PYTH_SCRIPTSDIR])
AC_SUBST([PYTH_SCRIPTSDIR_UNIX])
AC_SUBST([PYTHON])
AC_SUBST([SRCDIR_WIN])
AC_SUBST([WINDIR_UNIX])
AC_SUBST([Z3_PYC])

AC_CONFIG_FILES([Makefile setup.py])
AC_CONFIG_FILES([src/bin/analyze src/bin/gui])
AC_CONFIG_FILES([src/bin/gametime-cli.bat src/bin/gametime-gui.bat])
AC_CONFIG_FILES([src/cil/bin/cilly.bat src/config.xml])
AC_CHECK_FILE([src/phoenix/GameTime/Makefile.in],
    [AC_CONFIG_FILES([src/phoenix/GameTime/Makefile])], [])
AC_CHECK_FILE([src/phoenix/Utilities/Makefile.in],
    [AC_CONFIG_FILES([src/phoenix/Utilities/Makefile])], [])

AC_PROG_CC

AC_OUTPUT
