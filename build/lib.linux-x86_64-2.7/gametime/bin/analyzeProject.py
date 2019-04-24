#!/usr/bin/env python

"""Demonstrates many commonly-used key features of GameTime.

This script also provides a starting point to perform GameTime
analysis on code. The commonly-used features can be activated
through command-line arguments, whose explanation and usage is
described through the `--help` command-line argument.

However, not all features are provided by this script. Please
refer to the Python API for other available features, and
feel free to modify this script for your own purposes.
"""

"""See the LICENSE file, located in the root directory of
the source distribution and
at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
for details on the GameTime license and authors.
"""


import argparse
import os

from gametime import Analyzer, GameTime, GameTimeError, PathType
from gametime.defaults import logger, sourceDir, config
from gametime.interval import Interval
from gametime.fileHelper import createDir
from gametime.histogram import writeHistogramToFile
from gametime.projectConfiguration import readProjectConfigFile
from gametime.simulators.simulator import Simulator


def generateBasisPaths(projectConfig, analyzerLocation):
    """Demonstrates how to generate the :class:`~gametime.path.Path` objects
    that represent the basis paths of the code specified in a GameTime project
    configuration, represented by a
    :class:`~gametime.projectConfiguration.ProjectConfiguration` object.

    The function saves the information from the :class:`~gametime.path.Path`
    objects to different files in a temporary directory called `analysis`,
    created in the directory that contains the code being analyzed.
    The :class:`~gametime.analyzer.Analyzer` object that is used for analysis
    is saved to the location provided.

    Arguments:
        projectConfig:
            :class:`~gametime.projectConfiguration.ProjectConfiguration`
            object that represents the configuration of a GameTime project.
        analyzerLocation:
            Location where the :class:`~gametime.analyzer.Analyzer` object
            will be saved.
    """
    # Create a new :class:`~gametime.analyzer.Analyzer` object 
    # for this analysis.
    analyzer = GameTime.analyze(projectConfig)

    # Generate a list of the :class:`~gametime.path.Path` objects that
    # represent the basis paths of the code specified in the XML file.
    basisPaths = analyzer.generateBasisPaths()

    # To keep the filesystem clean, create a directory for the basis paths
    # within the temporary directory called `analysis`. Write
    # the information contained in the :class:`~gametime.path.Path` objects
    # to this directory.
    analysisDir = os.path.join(projectConfig.locationOrigDir, "analysis")
    basisDir = os.path.join(analysisDir, "basis")
    createDir(basisDir)
    analyzer.writePathsToFiles(paths=basisPaths, writePerPath=False,
                               rootDir=basisDir)

    # Save the analyzer for later use.
    analyzer.saveToFile(analyzerLocation)

def generatePaths(projectConfig, analyzerLocation, basisValuesLocation,
                  numPaths, pathType, interval, useObExtraction):
    """Demonstrates how to load an :class:`~gametime.analyzer.Analyzer`
    object, saved from a previous analysis, from a file, and how to load
    the values to be associated with the basis :class:`~gametime.path.Path`
    objects from a file.

    Once these values are loaded, the function generates as many feasible
    paths as possible, upper-bounded by the `numPaths` argument. This
    argument is ignored if the function is called to generate all of
    the feasible paths of the code being analyzed.

    The type of paths that will be generated is determined by
    the `pathType` argument, which is a class variable of
    the :class:`~gametime.pathGenerator.PathType` class. For
    a description of the types, refer to the documentation of
    the :class:`~gametime.pathGenerator.PathType` class.

    The function saves the information from the :class:`~gametime.path.Path`
    objects to different files in a temporary directory called `analysis`,
    created in the directory that contains the code being analyzed.
    The :class:`~gametime.analyzer.Analyzer` object that is used for analysis
    is saved back to the location provided. The predicted values of
    the feasible paths, stored as the values of the corresponding
    :class:`~gametime.path.Path` objects, are also saved to a file whose name
    has the prefix `predicted-`.

    Arguments:
        projectConfig:
            :class:`~gametime.projectConfiguration.ProjectConfiguration`
            object that represents the configuration of a GameTime project.
        analyzerLocation:
            Location of the saved :class:`~gametime.analyzer.Analyzer` object.
        basisValuesLocation:
            Location of the file that contains the values to be associated
            with the basis :class:`~gametime.path.Path` objects.
        pathType:
            Type of paths to generate, represented by a class variable of
            the :class:`~gametime.pathGenerator.PathType` class.
            The different types of paths are described in the documentation of
            the :class:`~gametime.pathGenerator.PathType` class.
        numPaths:
            Upper bound on the number of paths to generate.
        interval:
            Two-element tuple that represents the closed interval
            of values that the generated paths can have.
        useObExtraction:
            Boolean value specifiying whether to use overcomplete basis
            extraction algorithm

    Returns:
        List of :class:`~gametime.path.Path` objects that represent
        the feasible paths generated.
    """ 
    # Load an :class:`~gametime.analyzer.Analyzer` object from
    # a file saved from a previous analysis.
    analyzer = Analyzer.loadFromFile(analyzerLocation)

    # Load the values to be associated with the basis
    # :class:`~gametime.path.Path` objects from the file specified.
    analyzer.loadBasisValuesFromFile(basisValuesLocation)

    # Generate the list of the :class:`~gametime.path.Path` objects
    # that represent the feasible paths requested.
    paths = analyzer.generatePaths(
        numPaths, pathType, interval, useObExtraction)

    # To keep the filesystem clean, create a directory for these feasible
    # paths in the temporary directory called `analysis`. Write
    # the information contained in the :class:`~gametime.path.Path`
    # objects to this directory.
    projectConfig = analyzer.projectConfig
    analysisDir = os.path.join(projectConfig.locationOrigDir, "analysis")

    pathsDirName = PathType.getDescription(pathType)
    pathsDir = os.path.join(analysisDir, pathsDirName)
    createDir(pathsDir)
    analyzer.writePathsToFiles(paths, writePerPath=False, rootDir=pathsDir)

    # Get a one-word description of the path type.
    pathTypeDesc = PathType.getDescription(pathType)

    # Construct the location of the file that will store
    # the predictions of the values of the feasible paths.
    # Write the predicted values to this file.
    predictedValuesFileName = "predicted-%s" % pathTypeDesc
    predictedValuesLocation = os.path.join(analysisDir,
                                           predictedValuesFileName)
    Analyzer.writePathValuesToFile(paths, predictedValuesLocation)
   
    # Write the computed delta and the deltaMultiple into respective files
    if useObExtraction:
        Analyzer.writeValueToFile(
            analyzer.inferredMuMax, os.path.join(analysisDir, "mu-max"))
        Analyzer.writeValueToFile(
            analyzer.errorScaleFactor,
            os.path.join(analysisDir, "error-scale-factor"))

    # Save the analyzer for later use.
    analyzer.saveToFile(analyzerLocation)

    return paths

def measureBasisPaths(analyzerLocation, simulator):
    """Demonstrates how to load an :class:`~gametime.analyzer.Analyzer` object,
    saved from a previous analysis, from a file, how to measure the values of
    the feasible basis paths, represented by :class:`~gametime.path.Path`
    objects, on a simulator, and how to save these values to a file.

    Arguments:
        analyzerLocation:
            Location of the saved :class:`~gametime.analyzer.Analyzer` object.
        simulator:
            :class:`~gametime.simulators.Simulator` object that represents
            the simulator on which the values of the feasible basis paths
            will be measured.
    """
    # Load an :class:`~gametime.analyzer.Analyzer` object
    # from a file saved from a previous analysis.
    analyzer = Analyzer.loadFromFile(analyzerLocation)

    # Construct the location of the file that will store
    # the measurements of the values of the feasible basis paths.
    # Perform the measurements and write the values to this file.
    projectConfig = analyzer.projectConfig
    analysisDir = os.path.join(projectConfig.locationOrigDir, "analysis")
    measuredValuesLocation = os.path.join(analysisDir, "measured-basis")
    simulator.writeMeasurementsToFile(measuredValuesLocation,
                                      analyzer.basisPaths)

    # Save the analyzer for later use.
    analyzer.saveToFile(analyzerLocation)

def measurePaths(projectConfig, simulator, paths, pathType):
    """Demonstrates how to measure the values of a list of feasible
    paths, represented by :class:`~gametime.path.Path` objects,
    on a simulator, and how to save these measured values to a file.

    Arguments:
        projectConfig:
            :class:`~gametime.projectConfiguration.ProjectConfiguration`
            object that represents the configuration of a GameTime project.
        simulator:
            :class:`~gametime.simulators.Simulator` object that
            represents the simulator on which the values of
            the feasible paths, represented by
            :class:`~gametime.path.Path` objects, will be measured.
        paths:
            List of feasible paths, represented by
            :class:`~gametime.path.Path` objects,
            whose values are to be measured.
        pathType:
            Type of the feasible paths, represented by a class variable of
            the :class:`~gametime.pathGenerator.PathType` class.
            The different types of paths are described in the documentation of
            the :class:`~gametime.pathGenerator.PathType` class.
    """
    # Get a one-word description of the path type.
    pathTypeDesc = PathType.getDescription(pathType)

    # Construct the location of the file that will store
    # the measurements of the values of the feasible paths.
    # Perform the measurements and write the values to this file.
    analysisDir = os.path.join(projectConfig.locationOrigDir, "analysis")
    measuredValuesFileName = "measured-%s" % pathTypeDesc
    measuredValuesLocation = os.path.join(analysisDir,
                                          measuredValuesFileName)
    simulator.writeMeasurementsToFile(measuredValuesLocation, paths)

def _getHistogramRange(paths, lower=None, upper=None, measured=False):
    """Gets the range of values for the histogram that will be created
    from the values of the list of feasible paths provided, each of which
    is represented by a :class:`~gametime.path.Path` object.

    If either a lower bound or an upper bound is already provided as
    optional arguments, this helper function also checks if there are
    any values that lie either below the lower bound or above
    the upper bound, and warns the user accordingly.

    If the lower bound is not provided, the smallest value in
    the provided list is considered the lower bound; similarly,
    if the upper bound is not provided, the greatest value in
    the provided list is considered the upper bound.

    Arguments:
        paths:
            List of :class:`~gametime.path.Path` objects,
            each of which represents a feasible path.
        lower:
            Lower bound of the range of values for the histogram.
        upper:
            Upper bound of the range of values for the histogram.
        measured:
            `True` if, and only if, the values that will be used for
            the histogram are the measured values of the feasible paths.
    Returns:
        Tuple whose first element is the lower bound of the range of values
        for the histogram and whose second element is the upper bound of
        values for the histogram.
    """
    pathValues = [path.measuredValue if measured else path.predictedValue
                  for path in paths]

    warnMsgTemplate = ("WARNING: There are %d paths whose values are %s "
                       "than the %s bound provided. These paths will be "
                       "ignored in the histogram creation.")
    if lower is not None:
        smallerThanBound = sum([1 for value in pathValues if value < lower])
        if smallerThanBound > 0:
            warnMsg = warnMsgTemplate % (smallerThanBound, "smaller", "lower")
            logger.warn(warnMsg)
    if upper is not None:
        greaterThanBound = sum([1 for value in pathValues if value > upper])
        if greaterThanBound > 0:
            warnMsg = warnMsgTemplate % (greaterThanBound, "greater", "upper")
            logger.warn(warnMsg)

    if lower is None or upper is None:
        lower = lower or min(pathValues)
        upper = upper or max(pathValues)
    elif lower is not None and upper is not None:
        lower, upper = sorted((lower, upper))
    return (lower, upper)

def createHistogramForBasisPaths(analyzerLocation, numBins, lower, upper):
    """Demonstrates how to load an :class:`~gametime.analyzer.Analyzer` object,
    saved from a previous analysis, from a file, how to create a histogram
    from the measured values of the feasible basis paths, represented by
    :class:`~gametime.path.Path` objects, and how to save this histogram
    to a file.

    Arguments:
        analyzerLocation:
            Location of the saved :class:`~gametime.analyzer.Analyzer` object.
        numBins:
            Number of bins in the histogram created.
        lower:
            Lower bound of the range of values for the histogram.
        upper:
            Upper bound of the range of values for the histogram.
    """
    # Load an :class:`~gametime.analyzer.Analyzer` object
    # from a file saved from a previous analysis.
    analyzer = Analyzer.loadFromFile(analyzerLocation)
    basisPaths = analyzer.basisPaths

    # Construct the location of the file that will store the histogram
    # created from the measured values of the feasible basis paths.
    # Create the histogram and write it to this file.
    projectConfig = analyzer.projectConfig
    analysisDir = os.path.join(projectConfig.locationOrigDir, "analysis")
    histogramLocation = os.path.join(analysisDir, "histogram-basis")

    basisPaths = analyzer.basisPaths
    range = _getHistogramRange(basisPaths, lower, upper, True)
    writeHistogramToFile(histogramLocation, basisPaths, numBins, range, True)

    # Save the analyzer for later use.
    analyzer.saveToFile(analyzerLocation)

def createHistogramForPaths(projectConfig, paths, pathType,
                            numBins, lower, upper, measured=False):
    """Demonstrates how to create a histogram from the values of
    feasible paths, and how to save this histogram to a file.

    Arguments:
        projectConfig:
            :class:`~gametime.projectConfiguration.ProjectConfiguration`
            object that represents the configuration of a GameTime project.
        paths:
            List of feasible paths, represented by
            :class:`~gametime.path.Path` objects,
            whose values will be used for the histogram.
        pathType:
            Type of the feasible paths, represented by a class variable of
            the :class:`~gametime.pathGenerator.PathType` class.
            The different types of paths are described in the documentation of
            the :class:`~gametime.pathGenerator.PathType` class.
        numBins:
            Number of bins in the histogram created.
        lower:
            Lower bound of the range of values for the histogram.
        upper:
            Upper bound of the range of values for the histogram.
        measured:
            `True` if, and only if, the values that will be used for
            the histogram are the measured values of the feasible paths.
    """
    # Get a one-word description of the path type.
    pathTypeDesc = ("basis" if pathType is None
                    else PathType.getDescription(pathType))

    # Construct the location of the file that will store
    # the histogram created from the values of the feasible
    # paths. Create the histogram and write it to this file.
    analysisDir = os.path.join(projectConfig.locationOrigDir, "analysis")
    histogramFileName = ("histogram-%s-%s" %
                         ("measured" if measured else "predicted",
                          pathTypeDesc))
    histogramLocation = os.path.join(analysisDir, histogramFileName)

    range = _getHistogramRange(paths, lower, upper, measured)
    writeHistogramToFile(histogramLocation, paths, numBins, range, measured)

def _createArgParser():
    """
    Returns:
        Argument parser for this demonstration file.
    """
    workingDir = os.getcwd()
    argParser = argparse.ArgumentParser(
        description=("Demonstration of GameTime analysis. For arguments that "
                     "expect a location on the filesystem, relative locations "
                     "are resolved against the current working directory, "
                     "located at %s." % workingDir)
    )

    analysisGroupTitle = "Optional arguments for analysis"
    analysisGroupDesc = "Options to initialize and save the GameTime analysis."
    analysisGroup = argParser.add_argument_group(analysisGroupTitle,
                                                 analysisGroupDesc)

    currDir = os.path.dirname(os.path.abspath(__file__))
    analysisGroup.add_argument(
        "-a", "--analyze", metavar="LOCATION",
        default=os.path.normpath(
            os.path.join(currDir, "../demo/modexp_unrolled/projectConfig.xml")
        ),
        help=("Location of the XML file that configures a GameTime project. "
              "The location of a directory can also be provided, in which "
              "case the first XML file found in the directory will be used to "
              "configure a GameTime project. If this argument is not "
              "provided, the XML file located at %(default)s will be used.")
    )

    analysisGroup.add_argument(
        "--analyzer-location", metavar="LOCATION",
        help=("Location where the analyzer is saved. If this argument is not "
              "provided, the analyzer is saved to a file called `analyzer` "
              "in a temporary directory called `analysis`, created within "
              "the directory that contains the code to analyze.")
    )

    preprocessingGroupTitle = "Optional arguments for preprocessing"
    preprocessingGroupDesc = "Options to preprocess the code to analyze."
    preprocessingGroup = argParser.add_argument_group(preprocessingGroupTitle,
                                                      preprocessingGroupDesc)

    preprocessingGroup.add_argument(
        "--unroll-loops", action="store_true",
        help=("Unrolls the loops in the code under analysis, using the bounds "
              "provided by the user in the loop configuration file generated "
              "during loop detection. The use of this option overrides "
              "the corresponding tag (<unroll-loops>) in the project "
              "configuration XML file.")
    )

    basisGroupTitle = "Optional arguments for feasible basis paths"
    basisGroupDesc = "Options to generate basis paths."
    basisGroup = argParser.add_argument_group(basisGroupTitle, basisGroupDesc)

    basisGroup.add_argument(
        "-b", "--basis", action="store_true",
        help=("Generates the basis paths of the code under analysis, and "
              "writes them to files in a temporary directory called "
              "`analysis`, created within the directory that contains "
              "the code to analyze.")
    )

    basisGroup.add_argument(
        "--overcomplete_basis", action="store_true",
        help=("Find overcomplete basis. The propertis of the overcomplete "
          "basis, such as how well it approximates other paths, are specified "
          "in the projectConfig files. This flags requires --ob_extraction "
          "to be set to true."))
    basisGroup.add_argument(
        "--ob_extraction", action="store_true",
        help=("Use new algorithm to extract the longest paths from the set of "
          "measurements."))


    feasibleGroupTitle = "Optional arguments for other types of feasible paths"
    feasibleGroupDesc = (
        "Options to generate other types of feasible paths, once "
        "the basis paths have been generated. All paths are also "
        "written to files in a temporary directory called `analysis`, "
        "created within the directory that contains the code to analyze, "
        "and the predicted values of these paths are also saved to "
        "a file whose name has the prefix `predicted-`."
    )
    feasibleGroup = argParser.add_argument_group(feasibleGroupTitle,
                                                 feasibleGroupDesc)

    locationDesc = (
        "Location of the file that contains the values to be associated "
        "with the basis paths. If this argument is not provided, "
        "this script assumes that this file is called `measured-basis`, "
        "located in the temporary directory called `analysis`."
    )
    feasibleGroup.add_argument("--values", metavar="FILE", help=locationDesc)
    feasibleGroup.add_argument(
        "-n", "--num-paths", metavar="NUM", type=int, default=5,
        help=("Upper bound on the number of paths to generate. This value is "
              "ignored if all feasible paths are being generated. "
              "The default value is %(default)s.")
    )

    pathDesc = "Generates %s feasible paths of the code under analysis%s."
    feasibleGroup.add_argument(
        "-w", "--worst-case", action="store_true",
        help=pathDesc % ("the worst-case", " (paths with the largest values)")
    )
    feasibleGroup.add_argument(
        "-v", "--best-case", action="store_true",
        help=pathDesc % ("the best-case", " (paths with the smallest values)")
    )
    feasibleGroup.add_argument(
        "-r", "--random", action="store_true",
        help=pathDesc % ("random", "")
    )
    feasibleGroup.add_argument(
        "-d", "--all-decreasing", action="store_true",
        help=pathDesc % (
            "all",
            (" (in decreasing order of value). WARNING: The total number of "
             "feasible paths can be very large")
        )
    )
    feasibleGroup.add_argument(
        "-i", "--all-increasing", action="store_true",
        help=pathDesc % (
            "all",
            (" (in increasing order of value). WARNING: The total number of "
             "feasible paths can be very large")
        )
    )

    boundDesc = (
        "%s bound on the values of the feasible paths that are generated. "
        "If this argument is not provided, no %s bound is assumed."
    )
    feasibleGroup.add_argument(
        "-l", "--lower", metavar="BOUND", type=float, default=None,
        help=(boundDesc % ("Lower", "lower"))
    )
    feasibleGroup.add_argument(
        "-u", "--upper", metavar="BOUND", type=float, default=None,
        help=(boundDesc % ("Upper", "upper"))
    )

    measurementGroupTitle = ("Optional arguments for measurement of "
                             "values of feasible paths")
    measurementGroupDesc = (
        "Options to measure the values of all types of feasible paths, "
        "including feasible basis paths. The files that store "
        "the measurements and predictions are located in the temporary "
        "directory called `analysis`."
    )
    measurementGroup = argParser.add_argument_group(measurementGroupTitle,
                                                    measurementGroupDesc)
    measurementGroup.add_argument(
        "-m", "--measure-tests", action="store_true",
        help=("Creates test cases for each feasible path generated, and "
              "measures the value of each test case on the specified "
              "simulator. This option can be used with any other option that "
              "generates feasible paths. The measurements are saved to a file "
              "whose name has the prefix `measured-`. If none of these other "
              "options are used, the test cases for the feasible basis paths "
              "are created and measured, and the measurements are saved to "
              "a file called `measured-basis`. However, this assumes that "
              "the feasible basis paths have already been generated.")
    )

    simulatorChoices = ["ptarm", "simit-arm"]
    measurementGroup.add_argument(
        "-s", "--simulator", choices=simulatorChoices, default="ptarm",
        help=("Simulator to measure the cycle counts of the test "
              "case files on. The default simulator is %(default)s.")
    )

    histogramGroupTitle = ("Optional arguments for creating histograms "
                           "from the values of feasible paths")
    histogramGroupDesc = (
        "Options to create histograms computed from the values of all "
        "types of feasible paths, including feasible basis paths. "
        "The files that store the histograms are located in the temporary "
        "directory called `analysis`."
    )
    histogramGroup = argParser.add_argument_group(histogramGroupTitle,
                                                  histogramGroupDesc)
    histogramGroup.add_argument(
        "--histogram", action="store_true",
        help=("Creates a histogram from the values of feasible paths. "
              "This option can be used with any other option that generates "
              "feasible paths. The histogram created from the predicted "
              "values of these feasible paths is saved to a file whose name "
              "has the prefix `histogram-predicted-`. If the values of these "
              "feasible paths are also measured on a simulator, using "
              "the command-line options for measurement, a histogram is "
              "created from the measured values and saved to a file whose "
              "name has the prefix `histogram-measured-`. If, however, none "
              "of the other options to generate feasible paths are used, "
              "or if feasible basis paths are being generated (and optionally "
              "measured), a histogram is created from the values of "
              "the feasible basis paths and saved to a file called "
              "`histogram-basis`.")
    )
    histogramGroup.add_argument(
        "--hist-bins", metavar="SIZE", type=int, default=10,
        help=("Number of equally-sized bins in the histogram that is "
              "created. If not provided, the histogram will have %(default)s "
              "equally-sized bins. This command-line option can only be used "
              "along with the `--histogram` command-line option.")
    )
    histogramGroup.add_argument(
        "--hist-lower", metavar="SIZE", type=float, default=None,
        help=("Lower bound of the range of path values to be considered "
              "for histogram creation. If not provided, the lower bound is "
              "the smallest value of the paths that are considered for "
              "histogram creation. This command-line option can only "
              "be used along with the `--histogram` command-line option. ")
    )
    histogramGroup.add_argument(
        "--hist-upper", metavar="SIZE", type=float, default=None,
        help=("Upper bound of the range of path values to be considered "
              "for histogram creation. If not provided, the upper bound is "
              "the greatest value of the paths that are considered for "
              "histogram creation. This command-line option can only "
              "be used along with the `--histogram` command-line option. ")
    )

    return argParser

def _findXmlFile(location):
    """
    Arguments:
        location:
            Location of either an XML file or a directory.

    Returns:
        Location of an XML file: If the location provided is already one of
        an XML file, that location is returned; otherwise, the location
        provided is assumed to be a directory, and the location of the first
        XML file within the directory is returned. If no XML file can be found,
        this function returns `None`.
    """
    _, extension = os.path.splitext(location)
    if extension == ".xml":
        return os.path.normpath(location)
    if os.path.exists(location) and os.path.isdir(location):
        for entry in os.listdir(location):
            entry = os.path.normpath(os.path.join(location, entry))
            if os.path.isfile(entry):
                _, extension = os.path.splitext(entry)
                if extension == ".xml":
                    return entry

def _getSimulator(name, projectConfig):
    """
    Arguments:
        name:
            Name of a simulator.
        projectConfig:
            :class:`~gametime.projectConfiguration.ProjectConfiguration`
            object that represents the configuration of a GameTime project.

    Returns:
        :class:`~gametime.simulators.Simulator` object that represents
        the simulator whose name is provided.
    """
    from gametime.simulators.ptarmSimulator import PtarmSimulator
    from gametime.simulators.simItArmSimulator import SimItArmSimulator
    return {
        "ptarm": PtarmSimulator(projectConfig),
        "simit-arm": SimItArmSimulator(projectConfig),
    }[name]

def main():
    """Main function invoked when this script is run."""
    argParser = _createArgParser()
    args = argParser.parse_args()

    argsAndPathTypes = [
        (args.worst_case, PathType.WORST_CASE),
        (args.best_case, PathType.BEST_CASE),
        (args.random, PathType.RANDOM),
        (args.all_decreasing, PathType.ALL_DECREASING),
        (args.all_increasing, PathType.ALL_INCREASING)
    ]
    pathTypeArgs = [pathTypeArg for pathTypeArg, _ in argsAndPathTypes]
    pathTypes = [pathType for _, pathType in argsAndPathTypes]

    # Require the use of new_extraction algorithm when used with overcomplete
    # basis
    if ((args.overcomplete_basis and any(pathTypeArgs))
        and not (args.ob_extraction)):
        raise GameTimeError("New extraction algorithm must be used when over "
                            "complete basis is requested")

    # Proceed only if either the generation of feasible paths, or
    # the measurement of the values of feasible paths, or the creation
    # of a histogram of path values has been requested.
    if (not args.basis and not any(pathTypeArgs)
        and not args.measure_tests and not args.histogram):
        return

    # Find the XML file that will be used to initialize the GameTime project.
    workingDir = os.getcwd()
    location = os.path.normpath(os.path.join(workingDir, args.analyze))
    projectConfigXmlFile = _findXmlFile(location)
    if not projectConfigXmlFile:
        raise GameTimeError("No XML file for the configuration of "
                            "a GameTime project was found at %s." %
                            location)

    # Initialize a :class:`~gametime.projectConfiguration.ProjectConfiguration`
    # object, which represents the configuration of a GameTime project, with
    # the contents of the XML file.
    projectConfig = readProjectConfigFile(projectConfigXmlFile)
    if args.unroll_loops:
        projectConfig.UNROLL_LOOPS = args.unroll_loops

    if args.overcomplete_basis:
        projectConfig.OVER_COMPLETE_BASIS = True

    # Create a temporary directory called `analysis` in
    # the directory that contains the code being analyzed.
    analysisDir = os.path.join(projectConfig.locationOrigDir, "analysis")
    createDir(analysisDir)

    # Determine the location where
    # the :class:`~gametime.analyzer.Analyzer` object used for
    # analysis will be saved and loaded from.
    analyzerLocation = (args.analyzer_location or
                        os.path.join(analysisDir, "analyzer"))

    # Generate the feasible basis paths, if requested.
    if args.basis:
        generateBasisPaths(projectConfig, analyzerLocation)

    # If measurement of feasible paths is requested but no type has been
    # provided, or the generation of feasible basis paths is also requested,
    # measure the values of the feasible basis paths.
    if (args.measure_tests and (args.basis or not any(pathTypeArgs))):
        simulator = _getSimulator(args.simulator, projectConfig)
        measureBasisPaths(analyzerLocation, simulator)

    # If the creation of a histogram is requested but no type has been
    # provided, or the generation of feasible basis paths is also requested,
    # create a histogram from the values of the feasible basis paths.
    if (args.histogram and (args.basis or not any(pathTypeArgs))):
        createHistogramForBasisPaths(analyzerLocation, args.hist_bins,
                                     args.hist_lower, args.hist_upper)

    # Generate other types of feasible paths, if requested.
    basisValuesLocation = (os.path.join(workingDir, args.values) if args.values
                           else os.path.join(analysisDir, "measured-basis"))
    numPaths = args.num_paths
    interval = Interval(args.lower, args.upper)
    useObExtraction = True if args.ob_extraction else False

    for pathTypeArg, pathType in argsAndPathTypes:
        if pathTypeArg:
            paths = generatePaths(projectConfig, analyzerLocation,
                                  basisValuesLocation, numPaths, pathType,
                                  interval, useObExtraction)

            # Create a histogram of the predicted values of
            # the feasible paths generated, if requested.
            if args.histogram:
                createHistogramForPaths(projectConfig, paths, pathType,
                                        args.hist_bins, args.hist_lower,
                                        args.hist_upper)

            # Measure the feasible paths generated, if requested.
            if args.measure_tests:
                simulator = _getSimulator(args.simulator, projectConfig)
                measurePaths(projectConfig, simulator, paths, pathType)

            # Create a histogram of the measured values of
            # the feasible paths generated, if requested.
            if args.measure_tests and args.histogram:
                createHistogramForPaths(projectConfig, paths, pathType,
                                        args.hist_bins, args.hist_lower,
                                        args.hist_upper, measured=True)


if __name__ == "__main__":
    main()
