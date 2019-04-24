#!/usr/bin/env python

"""Exposes classes and functions that maintain the graphical
user interface for GameTime.
"""

"""See the LICENSE file, located in the root directory of
the source distribution and
at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
for details on the GameTime license and authors.
"""


import os
import subprocess
import sys
import threading
import webbrowser

from PySide import QtCore
from PySide import QtGui
from PySide.QtCore import Qt
from PySide.QtCore import Signal

import gametime.gui.guiHelper
from gametime.gui.guiHelper import BasisValuesDialog
from gametime.gui.guiHelper import LoopBoundsDialog
from gametime.gui.guiHelper import ConfirmationDialog
from gametime.gui.guiHelper import MessageDialog
from gametime.gui.guiHelper import BasisMessageDialog
from gametime.gui.guiHelper import BasisGenerationDialog
from gametime.gui.guiHelper import ExceptionMessageBox
from gametime.gui.guiHelper import HistogramDialog
from gametime.gui.guiHelper import FileItem
from gametime.gui.guiHelper import FileSelectList
from gametime.gui.guiHelper import GenericAnalyzer
from gametime.gui.guiHelper import Highlighter
from gametime.gui.guiHelper import Loader
from gametime.gui.guiHelper import NumPathsDialog
from gametime.gui.guiHelper import AllPathsDialog
from gametime.gui.guiHelper import Saver
from gametime.gui.guiHelper import TextEditObject
from gametime.gui.guiHelper import Window
from gametime.gui.guiHelper import XmlFileDialog
from gametime.projectConfiguration import ProjectConfiguration
from gametime.projectConfiguration import readProjectConfigFile
from gametime.updateChecker import isUpdateAvailable


class GameTimeGui(QtGui.QMainWindow):
    """
    The GUI main window. Inherits QtGui.QMainWindow. Maintains any actions
    dealing with the menubar, console, and adding/deleting widgets.
    """

    def __init__(self):
        """
        Initializes the main window and creates all the necessary widgets in
        the main window: a menubar, a file selection sidebar, a status console,
        two text displays, and a status bar.

        A quick note: All main windows have a centralWidget, a menuBar
        (accessible through self.menuBar()), and optional dockWidgets.
        DockWidgets must be created as dockWidgets with empty content, and
        widgets like QtGui.QTextEdit and QListView are substituted in for
        content.
        """
        super(GameTimeGui, self).__init__()
        self.setWindowTitle("GameTime")
        self.showFullScreen()
        self.showMaximized()

        self.tempFiles = set([])

        self.guiThread = threading.currentThread()
        sys.excepthook = self.handleException

        ### PARAMETERS ###
        #: Left and right text display widgets, respectively.
        self.leftTextEdit = None
        self.rightTextEdit = None

        #: File select widget.
        self.fileSelectWidget = None

        #: Console dock widget.
        self.consoleWidget = None

        #: Queue of functions to analyze. This allows slots to run
        #: prerequisite slots while still allowing the user to have
        #: control over the GUI.
        self.funcQueue = []

        #: Cache of all currently open FileItem objects. These will all be
        #: displayed in the leftmost display.
        #: Key: {string} Name of a unique file.
        #: Value: {FileItem} FileItem object that corresponds to a unique file.
        self.openItems = {}

        #: List of actions that should be disabled while analysis is running.
        self.analysisActions = []
        #: List of actions that can be performed if overcomplete basis was
        #: generated
        self.overcompleteSupportedActions = []

        #: Specifies whether an overcomplete basis has been generated, so that
        #: we show only menu items that can handle overcomplete basis
        self.generatedOvercompleteBasis = False

        ### SETUP ###
        #: Set up text displays.
        self.setupCenterLayout()

        #: Set up file select widget, console widget and menubar widget.
        self.setupFileSelect()
        self.setupConsole()
        self.setupMenubar()

        #: Show status bar. In this case, the status bar serves more as an
        #: informational bar about certain actions, such as menubar action
        #: descriptions.
        self.statusBar().showMessage("Ready")

        #: Toggle for highlights (default on).
        self.highlightsEnabled = True

        self.analysisThread = WorkerThread(self, None)
        threadSignals = self.analysisThread.signals
        threadSignals.doneAnalyzing.connect(self.slotFinishAnalysis)
        threadSignals.showLoopDialog.connect(self.slotShowLoopDialog)
        threadSignals.printToConsole.connect(self.printToConsole)
        threadSignals.showException.connect(self.showException)
        self.showExceptionSignal = threadSignals.showException

        self._checkForAvailableUpdates()

    def handleException(self, eType, eInstance, tb):
        import traceback
        className = "%s: " % eType.__name__
        message = eInstance.message
        stackTrace = "".join(traceback.format_tb(tb))
        detailedTrace = "%s%s%s" % (stackTrace, className, message)

        if self.guiThread == threading.currentThread():
            self.showException(message, detailedTrace)
        else:
            self.showExceptionSignal.emit(message, detailedTrace)

    def showException(self, message, detailedTrace):
        ExceptionMessageBox(message,
                            ("Traceback (most recent call last):\n%s" %
                             detailedTrace)).exec_()

    def setupCenterLayout(self):
        """
        The centralWidget here is actually a generic Widget with
        a QtGui.QHBoxLayout (horizontal layout) of two TextEditObjects
        placed side-by-side. Documentation and code for TextEditObject
        is available in guiHelper.py.
        Initializes leftTextEdit and rightTextEdit.
        """
        centerWindow = QtGui.QWidget()
        centerLayout = QtGui.QHBoxLayout()

        self.leftTextEdit = TextEditObject("")
        self.leftTextEdit.setMainWindow(self)
        self.leftTextEdit.setLineWrapMode(QtGui.QTextEdit.NoWrap)

        self.rightTextEdit = TextEditObject("")
        self.rightTextEdit.setMainWindow(self)
        self.rightTextEdit.setLineWrapMode(QtGui.QTextEdit.NoWrap)

        # Splitter allows for windows to be resized.
        centerSplitter = QtGui.QSplitter()
        centerSplitter.addWidget(self.leftTextEdit)
        centerSplitter.addWidget(self.rightTextEdit)
        centerSplitter.setChildrenCollapsible(False)
        centerLayout.addWidget(centerSplitter)

        centerWindow.setLayout(centerLayout)
        centerWindow.show()

        self.setCentralWidget(centerWindow)

    def setupFileSelect(self):
        """
        Sets up fileSelectWidget, the leftmost dockWidget.
        This widget displays all the files that have been opened or created
        druing execution, such as those opened by the user or created
        through GameTime-related actions. Documentation and code
        for FileSelectList is available in guiHelper.py.
        """
        self.fileSelectWidget = QtGui.QDockWidget("Currently open")
        fileSelect = FileSelectList(self)
        self.fileSelectWidget.setWidget(fileSelect)
        self.addDockWidget(Qt.DockWidgetArea(Qt.LeftDockWidgetArea),
                           self.fileSelectWidget)

    def setupConsole(self):
        """
        Set up consoleWidget, the dock widget at the bottom of the main window.
        consoleWidget contains a read-only console, which displays errors,
        file analysis statuses, and so on. Creates self.consoleWidget.
        """
        self.consoleWidget = QtGui.QDockWidget("Console")
        console = QtGui.QTextEdit()
        console.setReadOnly(True)
        self.consoleWidget.setWidget(console)

        self.addDockWidget(Qt.DockWidgetArea(Qt.BottomDockWidgetArea),
                           self.consoleWidget)

    def setupMenubar(self):
        """Creates the menu bar."""
        menubar = self.menuBar()

        fileMenu = menubar.addMenu("&File")

        openAction = QtGui.QAction("Open project...", self)
        openAction.setShortcut("Ctrl+O")
        openAction.setStatusTip("Open a C file or open an XML file to "
                                "configure a project for GameTime analysis.")
        openAction.triggered.connect(self.slotOpenProjectDialog)

        loadAction = QtGui.QAction("Load state...", self)
        loadAction.setShortcut("Ctrl+L")
        loadAction.setStatusTip("Load the saved state of the GameTime GUI for "
                                "a GameTime analysis from a previous session.")
        loadAction.triggered.connect(self.slotLoadStateDialog)

        saveAction = QtGui.QAction("Save state...", self)
        saveAction.setShortcut("Ctrl+S")
        saveAction.setStatusTip("Save the state of the GameTime GUI for "
                                "the current GameTime analysis.")
        saveAction.triggered.connect(self.slotSaveStateDialog)

        resetAction = QtGui.QAction("Reset state", self)
        resetAction.setStatusTip("Resets the state of the GameTime GUI for "
                                 "the current GameTime analysis.")
        resetAction.triggered.connect(self.slotResetAction)
        self.analysisActions.append(resetAction)
        self.overcompleteSupportedActions.append(resetAction)

        changeConfigAction = QtGui.QAction("Change configuration...", self)
        # changeConfigAction.setShortcut("Ctrl+C")
        changeConfigAction.setStatusTip("Change the configuration of "
                                        "the current project.")
        changeConfigAction.triggered.connect(self.slotChangeConfigDialog)

        saveConfigAction = QtGui.QAction("Save configuration...", self)
        # saveConfigAction.setShortcut("Ctrl+S")
        saveConfigAction.setStatusTip("Save the configuration of the current "
                                      "project to an XML file.")
        saveConfigAction.triggered.connect(self.slotSaveConfigDialog)

        closeAction = QtGui.QAction("Close project", self)
        closeAction.setStatusTip("Close the current project.")
        closeAction.triggered.connect(self.slotCloseAction)
        self.analysisActions.append(closeAction)
        self.overcompleteSupportedActions.append(closeAction)

        exitAction = QtGui.QAction("Exit", self)
        exitAction.setShortcut("Ctrl+Q")
        exitAction.setStatusTip("Exit GameTime.")
        exitAction.triggered.connect(QtGui.qApp.quit)

        fileMenu.addAction(openAction)
        fileMenu.addSeparator()
        fileMenu.addAction(loadAction)
        fileMenu.addAction(saveAction)
        fileMenu.addAction(resetAction)
        fileMenu.addSeparator()
        fileMenu.addAction(changeConfigAction)
        fileMenu.addAction(saveConfigAction)
        fileMenu.addSeparator()
        fileMenu.addAction(closeAction)
        fileMenu.addAction(exitAction)

        # runMenu: Generate basis paths and feasible paths.
        runMenu = menubar.addMenu("&Run")

        basisPathsAction = QtGui.QAction("Generate basis paths", self)
        basisPathsAction.setShortcut("Ctrl+B")
        # basisPathsAction.setStatusTip()
        basisPathsAction.triggered.connect(self.slotFindBasisPaths)

        worstCasesAction = QtGui.QAction("Generate worst-case feasible paths",
                                         self)
        worstCasesAction.setShortcut("Ctrl+W")
        # worstCasesAction.setStatusTip()
        worstCasesAction.triggered.connect(self.slotLongestCases)

        bestCasesAction = QtGui.QAction("Generate best-case feasible paths",
                                        self)
        bestCasesAction.setShortcut("Ctrl+V")
        # bestCasesAction.setStatusTip()
        bestCasesAction.triggered.connect(self.slotShortestCases)

        randomPathsAction = QtGui.QAction("Generate random feasible paths",
                                          self)
        randomPathsAction.setShortcut("Ctrl+R")
        randomPathsAction.triggered.connect(self.slotFindRandomPaths)

        allDecPathsAction = QtGui.QAction("Generate all feasible paths "
                                          "(decreasing order)",
                                          self)
        allDecPathsAction.setShortcut("Ctrl+D")
        allDecPathsAction.triggered.connect(self.slotAllPathsDec)

        allIncPathsAction = QtGui.QAction("Generate all feasible paths "
                                          "(increasing order)",
                                          self)
        allIncPathsAction.setShortcut("Ctrl+I")
        allIncPathsAction.triggered.connect(self.slotAllPathsInc)

        # writeWorstAction = QtGui.QAction("Write worst paths to files", self)
        # writeWorstAction.triggered.connect(self.slotWriteWorst)

        # writeBestAction = QtGui.QAction("Write best paths to files", self)
        # writeBestAction.triggered.connect(self.slotWriteBest)

        # writeRandomAction = QtGui.QAction("Write random paths to files", self)
        # writeRandomAction.triggered.connect(self.slotWriteRandom)

        # writeAllIncAction = QtGui.QAction("Write all feasible paths to files "
        #                                   "(increasing order)", self)
        # writeAllIncAction.triggered.connect(self.slotWriteAllInc)

        # writeAllDecAction = QtGui.QAction("Write all feasible paths to files "
        #                                   "(decreasing order)", self)
        # writeAllDecAction.triggered.connect(self.slotWriteAllDec)

        histogramAction = QtGui.QAction("Generate histogram", self)
        histogramAction.triggered.connect(self.slotGenerateHistogram)

        self.cancelAction = QtGui.QAction("Cancel current analysis", self)
        self.cancelAction.triggered.connect(self.slotCancelAction)

        self.analysisActions.append(basisPathsAction)
        self.analysisActions.append(worstCasesAction)
        self.analysisActions.append(bestCasesAction)
        self.analysisActions.append(randomPathsAction)
        self.analysisActions.append(allDecPathsAction)
        self.analysisActions.append(allIncPathsAction)

        self.overcompleteSupportedActions.append(basisPathsAction)
        self.overcompleteSupportedActions.append(worstCasesAction)
        self.overcompleteSupportedActions.append(bestCasesAction)
        self.overcompleteSupportedActions.append(allDecPathsAction)
        self.overcompleteSupportedActions.append(allIncPathsAction)
        self.overcompleteSupportedActions.append(randomPathsAction)

        # self.analysisActions.append(writeWorstAction)
        # self.analysisActions.append(writeBestAction)

        self.analysisActions.append(histogramAction)

        runMenu.addAction(basisPathsAction)
        runMenu.addSeparator()
        runMenu.addAction(worstCasesAction)
        runMenu.addAction(bestCasesAction)
        runMenu.addSeparator()
        runMenu.addAction(randomPathsAction)
        runMenu.addAction(allDecPathsAction)
        runMenu.addAction(allIncPathsAction)
        runMenu.addSeparator()
        runMenu.addAction(histogramAction)
        runMenu.addSeparator()
        # runMenu.addAction(writeWorstAction)
        # runMenu.addAction(writeBestAction)
        # runMenu.addSeparator()
        runMenu.addAction(self.cancelAction)

        editMenu = menubar.addMenu("&Edit")

        basisValuesAction = QtGui.QAction("Enter basis values...", self)
        basisValuesAction.setStatusTip(
            "Manually enter values for the basis paths or import "
            "a file that contains the values."
        )
        basisValuesAction.triggered.connect(self.slotBasisValuesDialog)

        # cutAction = QtGui.QAction("Add labels...", self)
        # cutAction.setStatusTip("Select a smaller section of the code "
        #                        "to analyze.")
        # cutAction.triggered.connect(self.slotCutDialog)

        saveBasisValuesAction = QtGui.QAction("Save basis values...", self)
        saveBasisValuesAction.setStatusTip("Save the basis values to a file.")
        saveBasisValuesAction.triggered.connect(self.slotSaveBasisValues)

        self.analysisActions.append(basisValuesAction)
        self.overcompleteSupportedActions.append(basisValuesAction)
        # self.analysisActions.append(cutAction)

        editMenu.addAction(basisValuesAction)
        # editMenu.addAction(cutAction)
        # editMenu.addAction(saveBasisValuesAction) # TODO: Buggy. This too.

        # viewMenu: File Select checkable, Console checkable
        viewMenu = menubar.addMenu("&View")

        highlightPath = QtGui.QAction("&Highlight path", self)
        highlightPath.triggered.connect(self.slotTogglePathHighlight)
        highlightPath.setCheckable(True)
        highlightPath.setChecked(True)

        zoomIn = QtGui.QAction("Increase font size", self)
        zoomIn.triggered.connect(self.zoomIn)
        zoomIn.setShortcut("Ctrl++")

        zoomOut = QtGui.QAction("Decrease font size", self)
        zoomOut.triggered.connect(self.zoomOut)
        zoomOut.setShortcut("Ctrl+-")

        viewMenu.addAction(self.fileSelectWidget.toggleViewAction())
        viewMenu.addAction(self.consoleWidget.toggleViewAction())
        viewMenu.addAction(highlightPath)
        viewMenu.addAction(zoomIn)
        viewMenu.addAction(zoomOut)

    def _checkForAvailableUpdates(self):
        from gametime.defaults import config

        updateAvailable, latestVersionInfo = isUpdateAvailable()
        if updateAvailable:
            version = latestVersionInfo["version"]
            infoUrl = latestVersionInfo["info_url"]

            updateAvailableMsg = ("An updated version of GameTime (%s) is "
                                  "available. The current version is %s." %
                                  (version, config.VERSION))
            updateAvailableMsgBox = QtGui.QMessageBox(
                QtGui.QMessageBox.Information,
                unicode("Update available"),
                updateAvailableMsg
            )

            updateAvailableInfoMsg = ("Would you like to download and install "
                                      "this version?")
            updateAvailableMsgBox.setInformativeText(updateAvailableInfoMsg)

            buttons = QtGui.QMessageBox.Ok
            buttons |= QtGui.QMessageBox.Cancel
            updateAvailableMsgBox.setStandardButtons(buttons)
            updateAvailableMsgBox.setDefaultButton(QtGui.QMessageBox.Ok)
            choice = updateAvailableMsgBox.exec_()

            if choice == QtGui.QMessageBox.Ok:
                try:
                    webbrowser.open(infoUrl)
                    sys.exit(0)
                except Exception:
                    browserNotOpenMsg = ("Unable to open a web browser "
                                         "to display information about "
                                         "the updated version.")
                    # TODO (jkotker): Display the Exception information.
                    browserNotOpenMsgBox = QtGui.QMessageBox(
                        QtGui.QMessageBox.Warning,
                        unicode("Unable to open web browser"),
                        browserNotOpenMsg
                    )
                    browserNotOpenMsgBox.setInformativeText(
                        "Please visit %s to download and install "
                        "the updated version." % infoUrl
                    )

                    buttons = QtGui.QMessageBox.Ok
                    browserNotOpenMsgBox.setStandardButtons(buttons)
                    browserNotOpenMsgBox.setDefaultButton(QtGui.QMessageBox.Ok)
                    choice = browserNotOpenMsgBox.exec_()
            elif choice == QtGui.QMessageBox.Cancel:
                downloadLaterMsg = "Update not installed."
                downloadLaterMsgBox = QtGui.QMessageBox(
                    QtGui.QMessageBox.Warning,
                    unicode("Update not installed"),
                    downloadLaterMsg
                )
                downloadLaterMsgBox.setInformativeText(
                    "Please visit %s to download and install "
                    "an updated version of GameTime." % infoUrl
                )

                buttons = QtGui.QMessageBox.Ok
                downloadLaterMsgBox.setStandardButtons(buttons)
                downloadLaterMsgBox.setDefaultButton(QtGui.QMessageBox.Ok)
                choice = downloadLaterMsgBox.exec_()
        elif not latestVersionInfo:
            self.printToConsole("Unable to obtain information about "
                                "available updates.")
            self.printToConsole("Please check %s for the latest version of "
                                "GameTime and for available updates." %
                                config.WEBSITE_URL)
        else:
            self.printToConsole("No updates to GameTime are available.")

    ### HELPER FUNCTIONS ###

    def zoomIn(self):
        self.changeFontSize(2)

    def zoomOut(self):
        self.changeFontSize(-2)

    def changeFontSize(self, amount):
        widgets = [
            QtGui.QApplication,
            self.rightTextEdit,
            self.leftTextEdit,
            self.consoleWidget.widget(),
            self.fileSelectWidget.widget()
        ]
        for widget in widgets:
            currentFont = widget.font()
            currentSize = currentFont.pointSize()
            if currentSize + amount > 0:
                currentFont.setPointSize(currentSize + amount)
            widget.setFont(currentFont)

    def printToConsole(self, message):
        """Prints the provided message to the console and scrolls the viewport
        if the viewport was not already at the bottom of the console.

        Arguments:
            message:
                Message to print.
        """
        console = self.consoleWidget.widget()
        console.append(str(message))

        vertBar = console.verticalScrollBar()
        if vertBar.value() == vertBar.maximum():
            vertBar.setValue(vertBar.maximum())

    def reset(self):
        """Resets the GUI to initial conditions. This is used when
        a new file is loaded.
        """
        self.fileSelectWidget.widget().clear()
        self.leftTextEdit.clear()
        self.rightTextEdit.clear()

    ### SLOTS ###
    def slotResetAction(self):
        fileSelect = self.fileSelectWidget.widget()
        currentFileItem = fileSelect.activeLeft.getAnalyzeItem()
        for _ in range(len(currentFileItem.children)):
            child = currentFileItem.children[0]
            currentFileItem.removeChild(child)
            fileSelect.removeItem(child)

    def slotOpenProjectDialog(self):
        """Creates a QFileDialog and obtains a file name, which it
        then passes to openFile.
        """
        fileDialog = QtGui.QFileDialog()
        fileDialog.setFileMode(QtGui.QFileDialog.ExistingFile)
        fileTypes = "XML files (*.xml);;C files (*.c)"
        fileName, _ = fileDialog.getOpenFileName(
            self,
            "Open File",
            ".",
            fileTypes
        )

        if not fileName:
            self.printToConsole("No file was selected to be opened.")
            return
        self.openFile(fileName)

    def openFile(self, fileName):
        """If the file name provided does not belong to a FileItem
        that was previously created, make a new FileItem. Then,
        set the new/existing FileItem to the left text display
        using addToWindow.
        """
        self.fileName = fileName
        _, fileNameExt = os.path.splitext(fileName)
        if fileNameExt in [".c", ".xml"]:
            if fileNameExt == ".xml":
                projectConfig = readProjectConfigFile(fileName)
            else:
                projectConfig = ProjectConfiguration(fileName, "", "z3")

            xmlDialog = XmlFileDialog(self, projectConfig)
            returnValue = xmlDialog.exec_()
            if returnValue == 0:
                self.printToConsole("GameTime analysis was cancelled "
                                    "for %s." % fileName)
                return
            else:
                self.printToConsole("GameTime project created and configured "
                                    "from %s." % fileName)
                projectConfig = xmlDialog.projectConfig
        else:
            self.printToConsole("Either a C file or a project configuration "
                                "XML file was expected.")
            return
        self._loadFromProjectConfig(projectConfig)

    def _loadFromProjectConfig(self, projectConfig):
        fileItemToAdd = None
        locationOrigFile = projectConfig.locationOrigFile
        displayName = projectConfig.nameOrigFile

        # If fileName is already in fileSelect,
        # bypass loading from disk.
        if displayName in self.openItems:
            fileItemToAdd = self.openItems[displayName]
        else:
            # If the fileName has not been encountered yet, create a new
            # FileItem object and load the proper data from disk.
            try:
                origFileHandler = open(locationOrigFile, "r")
            except EnvironmentError as err:
                errno, strerror = err
                self.printToConsole(
                    "I/O error({0}): {1}.".format(errno, strerror)
                )
            except Exception as errno:
                self.printToConsole("Error({0})".format(errno))
            finally:
                with origFileHandler:
                    fileText = origFileHandler.read()
                    fileItemToAdd = FileItem(displayName,
                                             locationOrigFile,
                                             fileText,
                                             self)
                    fileItemToAdd.addToMainWindow()
        fileItemToAdd.setProjectConfig(projectConfig)
        self.addToWindow(fileItemToAdd, Window.LEFT)

    def slotLoadStateDialog(self):
        loadSelect = Loader(self)
        if not loadSelect.exec_():
            self.printToConsole("No new GameTime GUI state was loaded.")

    def slotSaveStateDialog(self):
        """Opens a QFileDialog that specifies the name to which
        to save the file.
        """
        currentFile = self.fileSelectWidget.widget().activeLeft.getAnalyzeItem()
        saveSelect = Saver(self, currentFile)
        if not saveSelect.exec_():
            self.printToConsole("No GameTime GUI state was saved.")

    def slotChangeConfigDialog(self):
        currentFile = self.fileSelectWidget.widget().activeLeft.getAnalyzeItem()
        xmlDialog = XmlFileDialog(self, currentFile.projectConfig)
        val = xmlDialog.exec_()
        if val == 0:
            self.printToConsole("No change made to the configuration of "
                                "the project for the file located at %s." %
                                currentFile.originalName)
        else:
            currentFile.setProjectConfig(xmlDialog.projectConfig)
            self.printToConsole("Changes successfully made to "
                                "the configuration of the project for "
                                "the file located at %s." %
                                currentFile.originalName)

    def slotSaveConfigDialog(self):
        """Creates a QFileDialog and obtains a file location, where
        it then saves an XML file that contains the configuration of
        the current project.
        """
        projectConfig = self.leftTextEdit.fileItemObject.projectConfig

        fileDialog = QtGui.QFileDialog()
        fileName, _ = fileDialog.getSaveFileName(
            self,
            "Save project configuration to...",
            projectConfig.locationOrigDir,
            "XML files (*.xml)"
        )
        if not fileName:
            self.printToConsole("No file was selected to save "
                                "the configuration of the current project to.")
            return

        projectConfig.writeToXmlFile(fileName)
        self.printToConsole("Configuration of the current project was "
                            "saved to %s." % fileName)

    def slotCloseAction(self):
        """Closes the file currently in the left text display and
        any paths or subsections of it.
        """
        fileSelect = self.fileSelectWidget.widget()
        fileSelect.removeGroup(fileSelect.activeLeft)

    def slotFindBasisPaths(self):
        """Starts the generation of basis paths in a new thread,
        if possible. This allows the user to interact with the GUI
        while the GameTime analysis is running.
        """
        if self.leftTextEdit.fileItemObject is None:
            self.printToConsole("There is currently no file for which "
                                "GameTime can generate basis paths.")
            return

        itemToAnalyze = self.leftTextEdit.fileItemObject.getAnalyzeItem()
        if len(itemToAnalyze.children) > 0:
            val = ConfirmationDialog("This will delete all paths currently \n"
                                     "generated for this file. Are you sure \n"
                                     "you want to continue?").exec_()
            if val == 0:
                return
        basisAnalyzer = GenericAnalyzer(0, self)
        if BasisGenerationDialog(
            basisAnalyzer,
            itemToAnalyze.projectConfig.MAXIMUM_ERROR_SCALE_FACTOR).exec_() \
                ==1:
            itemToAnalyze.projectConfig.OVER_COMPLETE_BASIS = \
                basisAnalyzer.generateOvercompleteBasis
            itemToAnalyze.projectConfig.MAXIMUM_ERROR_SCALE_FACTOR = \
                basisAnalyzer.maximumErrorScaleFactor
            self.generatedOvercompleteBasis = \
                basisAnalyzer.generateOvercompleteBasis
        else: return
        
        for _ in range(len(itemToAnalyze.children)):
            child = itemToAnalyze.children[0]
            itemToAnalyze.removeChild(child)
            itemToAnalyze.fileList.removeItem(child)

        self.disableAnalysis()
        self.analysisThread.setAnalyzer(basisAnalyzer)
        self.analysisThread.setItem(itemToAnalyze)
        self.analysisThread.setFunc(self.findBasisPathsHelper, [basisAnalyzer])
        self.analysisThread.start()

    def findBasisPathsHelper(self, basisAnalyzer):
        """Function, which is run in the new thread, that handles running
        the analyzer to find basis paths.
        """
        return basisAnalyzer.exec_()

    def slotFindRandomPaths(self):
        """Begins generation of random feasible paths in a new thread,
        if possible. This allows the user to interact with the GUI while
        analysis is running. The only files that can have random paths are
        valid .c files that have been opened by the user.
        """
        if self.leftTextEdit.fileItemObject is None:
            self.printToConsole("There is currently no file for which GameTime "
                                "can generate random feasible paths.")
            return

        itemToAnalyze = self.leftTextEdit.fileItemObject.getAnalyzeItem()
        if itemToAnalyze.numBasisPaths == 0:
            self.printToConsole("Basis paths have not been generated for "
                                "this file. Generating the paths now...")
            self.slotFindBasisPaths()
            self.funcQueue.append(self.slotFindRandomPaths)
            return

        randomAnalyzer = GenericAnalyzer(3, self)
        if NumPathsDialog(randomAnalyzer, "Random").exec_() == 1:
            for _ in range(len(itemToAnalyze.randomPaths)):
                path = itemToAnalyze.randomPaths[0]
                itemToAnalyze.removeChild(path)
                itemToAnalyze.fileList.removeItem(path)

            self.disableAnalysis()
            self.analysisThread.setAnalyzer(randomAnalyzer)
            self.analysisThread.setItem(itemToAnalyze)
            self.analysisThread.setFunc(self.findPathsHelper, [randomAnalyzer])
            self.analysisThread.start()

    def slotAllPathsInc(self):
        """Begins generation of all paths, in order of increasing value,
        in a new thread, if possible. This allows the user to interact with
        the GUI while analysis is running. The only files that can have paths
        are valid .c files that have been opened by the user.
        """
        if self.leftTextEdit.fileItemObject is None:
            self.printToConsole("There is currently no file for which GameTime "
                                "can generate all feasible paths in order of "
                                "increasing value.")
            return

        itemToAnalyze = self.leftTextEdit.fileItemObject.getAnalyzeItem()
        if itemToAnalyze.numBasisPaths == 0:
            self.printToConsole("Basis paths have not been generated for "
                                "this file. Generating them now...")
            self.slotFindBasisPaths()
            self.funcQueue.append(self.slotAllPathsInc)
            return

        allPathAnalyzer = GenericAnalyzer(5, self)
        if AllPathsDialog(allPathAnalyzer,
            itemToAnalyze.projectConfig.OVER_COMPLETE_BASIS).exec_() == 1:
            for _ in range(len(itemToAnalyze.allPaths)):
                path = itemToAnalyze.allPaths[0]
                itemToAnalyze.removeChild(path)
                itemToAnalyze.fileList.removeItem(path)

            self.disableAnalysis()
            self.analysisThread.setAnalyzer(allPathAnalyzer)
            self.analysisThread.setItem(itemToAnalyze)
            self.analysisThread.setFunc(self.findPathsHelper, [allPathAnalyzer])
            self.analysisThread.start()

    def slotAllPathsDec(self):
        """Begins generation of all paths, in order of decreasing value,
        in a new thread, if possible. This allows the user to interact with
        the GUI while analysis is running. The only files that can have paths
        are valid .c files that have been opened by the user.
        """
        if self.leftTextEdit.fileItemObject is None:
            self.printToConsole("There is currently no file for which GameTime "
                                "can generate all feasible paths in order of "
                                "decreasing value.")
            return

        itemToAnalyze = self.leftTextEdit.fileItemObject.getAnalyzeItem()
        if itemToAnalyze.numBasisPaths == 0:
            self.printToConsole("Basis paths have not been generated for "
                                "this file. Generating them now...")
            self.slotFindBasisPaths()
            self.funcQueue.append(self.slotAllPathsDe)
            return

        allPathAnalyzer = GenericAnalyzer(4, self)
        if AllPathsDialog(allPathAnalyzer,
            itemToAnalyze.projectConfig.OVER_COMPLETE_BASIS).exec_() == 1:
            for _ in range(len(itemToAnalyze.allPaths)):
                path = itemToAnalyze.allPaths[0]
                itemToAnalyze.removeChild(path)
                itemToAnalyze.fileList.removeItem(path)

            self.disableAnalysis()
            self.analysisThread.setAnalyzer(allPathAnalyzer)
            self.analysisThread.setItem(itemToAnalyze)
            self.analysisThread.setFunc(self.findPathsHelper, [allPathAnalyzer])
            self.analysisThread.start()

    def slotWriteRandom(self):
        """
        Checks if the worst cases have already been generated for
        the currently active file.
        If they have been, then it just writes them to files.
        If they have not been, then it prompts the user how many they
        would like to generate and generates them before writing them
        to files.
        """
        currentFile = self.fileSelectWidget.widget().activeLeft.getAnalyzeItem()
        if not currentFile:
            self.printToConsole("Not a valid file to analyze")
            return
        if currentFile.getBasisPaths() == []:
            self.printToConsole("No basis paths have been generated "
                                "yet. Generating them now...")
            self.slotFindBasisPaths()
            self.funcQueue.append(self.slotWriteRandom)
            return
        if currentFile.getRandomPaths() == []:
            self.printToConsole("No random feasible paths have been generated "
                                "yet. Generating them now...")
            self.slotFindRandomPaths()
            self.funcQueue.append(self.slotWriteRandom)
            return

        fileDialog = QtGui.QFileDialog()
        fileNameChoice = fileDialog.getSaveFileName(
            self,
            "Save File",
            "."
        )

        pathText = ""
        for pathItem in currentFile.getRandomPaths():
            path = pathItem.getHighlightPath()
            for var, val in path.assignments.items():
                pathText += "%s=%s," % (var, val)
            pathText += "%s\n" % path.value
        if not fileNameChoice:
            self.printToConsole("No file was selected to save values to.")
            return

        fileWriter = open(fileNameChoice, "w")
        fileWriter.write(pathText)
        fileWriter.close()
        self.printToConsole("Random paths saved to %s" % fileNameChoice)

    def slotWriteAllInc(self):
        """
        Checks if the worst cases have already been generated for
        the currently active file.
        If they have been, then it just writes them to files.
        If they have not been, then it prompts the user how many they
        would like to generate and generates them before writing them
        to files.
        """
        currentFile = self.fileSelectWidget.widget().activeLeft.getAnalyzeItem()
        if not currentFile:
            self.printToConsole("Not a valid file to analyze.")
            return
        if currentFile.getBasisPaths() == []:
            self.printToConsole("No basis paths have been generated "
                                "yet. Generating them now...")
            self.slotFindBasisPaths()
            self.funcQueue.append(self.slotWriteAllInc)
            return
        if currentFile.getAllPaths() == []:
            self.printToConsole("All feasible paths have not been generated "
                                "yet. Generating them now...")
            self.slotFindAllPathsInc()
            self.funcQueue.append(self.slotWriteWriteAllInc)
            return

        fileDialog = QtGui.QFileDialog()
        fileNameChoice = fileDialog.getSaveFileName(
            self,
            "Save File",
            "."
        )

        pathText = ""
        for pathItem in currentFile.getAllPaths():
            path = pathItem.getHighlightPath()
            for var, val in path.assignments.items():
                pathText += "%s=%s," % (var, val)
            pathText += "%s\n" % path.value
        if not fileNameChoice:
            self.printToConsole("No file was selected to save values to.")
            return

        fileWriter = open(fileNameChoice, "w")
        fileWriter.write(pathText)
        fileWriter.close()
        self.printToConsole("All paths saved to %s" %fileNameChoice)

    def slotWriteAllDec(self):
        """
        Checks if the worst cases have already been generated for
        the currently active file.
        If they have been, then it just writes them to files.
        If they have not been, then it prompts the user how many they
        would like to generate and generates them before writing them
        to files.
        """
        currentFile = self.fileSelectWidget.widget().activeLeft.getAnalyzeItem()
        if not currentFile:
            self.printToConsole("Not a valid file to analyze.")
            return
        if currentFile.getBasisPaths() == []:
            self.printToConsole("No basis paths have been generated "
                                "yet. Generating them now...")
            self.slotFindBasisPaths()
            self.funcQueue.append(self.slotWriteAllDec)
            return
        if currentFile.getAllPaths() == []:
            self.printToConsole("All feasible paths, in order of decreasing "
                                "value, have not been generated yet. "
                                "Generating them now...")
            self.slotAllPathsDec()
            self.funcQueue.append(self.slotWriteAllDec)
            return

        fileDialog = QtGui.QFileDialog()
        fileNameChoice = fileDialog.getSaveFileName(
            self,
            "Save File",
            "."
        )

        pathText = ""
        for pathItem in currentFile.getAllPaths():
            path = pathItem.getHighlightPath()
            for var, val in path.assignments.items():
                pathText += "%s=%s," % (var, val)
            pathText += "%s\n" % path.value
        if not fileNameChoice:
            self.printToConsole("No file was selected to save values to.")
            return

        fileWriter = open(fileNameChoice, "w")
        fileWriter.write(pathText)
        fileWriter.close()
        self.printToConsole("All feasible paths saved to %s." % fileNameChoice)

    def findPathsHelper(self, analyzer):
        """Function, which is run in the new thread, that handles running
        the analyzer to find paths.
        """
        return analyzer.exec_()

    def slotCancelAction(self):
        if self.analysisThread.isRunning():
            if self.analysisThread.analyzer.enumCommand == 0:
                self.analysisThread.itemToAnalyze.numBasisPaths = 0
            self.analysisThread.terminate()
            self.printToConsole("Current analysis has been cancelled.")
        else:
            self.printToConsole("There was no analysis to cancel.")
        self.enableAnalysis()
        self.funcQueue = []

    def slotWriteBest(self):
        """Checks if the best case feasible paths have already been
        generated for the currently active file. If they have been generated,
        then it just writes them to files. If they have not been
        generated, then it prompts the user how many they would
        like to generate and generates them before writing them to files.
        """
        currentFile = self.fileSelectWidget.widget().activeLeft.getAnalyzeItem()
        if not currentFile:
            self.printToConsole("Not a valid file to analyze.")
            return
        if currentFile.getBasisPaths() == []:
            self.printToConsole("No basis paths have been generated "
                                "yet. Generating them now...")
            self.slotFindBasisPaths()
            self.funcQueue.append(self.slotWriteBest)
            return
        if currentFile.getBestPaths() == []:
            self.printToConsole("No best-case feasible paths have been "
                                "generated yet. Generating them now...")
            self.slotShortestCases()
            self.funcQueue.append(self.slotWriteBest)
            return
        # bestPaths = [path.getHighlightPath() for path
        #              in currentFile.getBestPaths()]

        # Open the file dialog to choose a new file to save to
        #Write the paths out, csv atm
        #Write the assignments then value to file
        fileDialog = QtGui.QFileDialog()
        fileNameChoice = fileDialog.getSaveFileName(
            self,
            "Save File",
            "."
        )

        pathText = ""
        for pathItem in currentFile.getBestPaths():
            path = pathItem.getHighlightPath()
            for var, val in path.assignments.items():
                pathText += "%s=%s," % (var, val)
            pathText += "%s\n" % path.value
        if not fileNameChoice:
            self.printToConsole("No file was selected to save values to.")
            return

        fileWriter = open(fileNameChoice, "w")
        fileWriter.write(pathText)
        fileWriter.close()
        self.printToConsole("Best paths saved to %s" % fileNameChoice)

    def slotWriteWorst(self):
        """
        Checks if the worst cases have already been generated for
        the currently active file.
        If they have been, then it just writes them to files.
        If they have not been, then it prompts the user how many they
        would like to generate and generates them before writing them
        to files.
        """
        currentFile = self.fileSelectWidget.widget().activeLeft.getAnalyzeItem()
        if not currentFile:
            self.printToConsole("Not a valid file to analyze.")
            return
        if currentFile.getBasisPaths() == []:
            self.printToConsole("No basis paths have been generated "
                                "yet. Generating them now...")
            self.slotFindBasisPaths()
            self.funcQueue.append(self.slotWriteWorst)
            return
        if currentFile.getWorstPaths() == []:
            self.printToConsole("No worst-case feasible paths have been "
                                "generated yet. Generating them now...")
            self.slotLongestCases()
            self.funcQueue.append(self.slotWriteWorst)
            return

        fileDialog = QtGui.QFileDialog()
        fileNameChoice = fileDialog.getSaveFileName(
            self,
            "Save File",
            "."
        )

        pathText = ""
        for pathItem in currentFile.getWorstPaths():
            path = pathItem.getHighlightPath()
            for var, val in path.assignments.items():
                pathText += "%s=%s," % (var, val)
            pathText += "%s\n" % path.value
        if not fileNameChoice:
            self.printToConsole("No file was selected to save values to.")
            return

        fileWriter = open(fileNameChoice, "w")
        fileWriter.write(pathText)
        fileWriter.close()
        self.printToConsole("Worst paths saved to %s" %fileNameChoice)

    def slotBasisValuesDialog(self):
        """
        Brings up a dialog that allows the user to either manually
        enter in values for each basis path or select a properly
        formatted file (-pathNum- -pathValue-\n) that contains values.
        """
        currentFile = self.fileSelectWidget.widget().activeLeft.getAnalyzeItem()
        if not currentFile:
            self.printToConsole("No file loaded to add basis values for.")
            return
        elif currentFile.numBasisPaths == 0:
            self.printToConsole("No basis paths have been generated yet. "
                                "Generating them now.")
            self.slotFindBasisPaths()
#            self.funcQueue.append(self.slotBasisValuesDialog)
            return
        value = BasisValuesDialog(self).exec_()
        if value == 1:
            self.printToConsole("Basis values entered.")
        else:
            self.printToConsole("No new basis values entered.")

    def slotSaveBasisValues(self):
        fileDialog = QtGui.QFileDialog()
        fileName, _ = fileDialog.getSaveFileName(self, "Save File", ".")

        # Check that values have been entered
        # if not valuesEntered:
        #     self.printToConsole("No values to save")
        #     return
        if not fileName:
            self.printToConsole("No file was selected to save values to.")
            return

        # Write values in correct format.
        currentFile = self.fileSelectWidget.widget().activeLeft.getAnalyzeItem()
        basisValues = currentFile.basisValues
        self.saveBasisValues(basisValues, fileName)

    def saveBasisValues(self, values, fileName):
        analyzer = self.analysisThread.analyzer
        for pathNum, basisPath in enumerate(analyzer.basisPaths):
            basisPath.value = values[pathNum]
        analyzer.writeBasisValuesToFile(fileName)
        self.printToConsole("Basis Values saved")

    def addToWindow(self, fileItemToAdd, window):
        """
        Add FileItem provided to left or right window, depending on which
        side is to be loaded next.

        Arguments:
            fileItemToAdd:
                FileItem to add to the window.
            window:
                Window to add the file item to.
        """
        # If fileItemToAdd was not previously displayed, print that
        # it was loaded. Otherwise, print nothing.
        if self.fileSelectWidget.widget().addFileName(fileItemToAdd, window):
            if not fileItemToAdd.getParent():
                self.printToConsole("Loaded %s." % fileItemToAdd.displayName)

    def slotShortestCases(self):
        """
        The action associated with accessing the menu bar item Run->Find Best
        Paths. Begins generation of best paths in a new thread if possible.
        This allows the user to interact with the GUI while analysis
        is running. The only files that can have best paths are valid .c files
        that have been opened by the user.
        """
        if self.leftTextEdit.fileItemObject is None:
            self.printToConsole("There is currently no file for which GameTime "
                                "can generate best-case feasible paths.")
            return

        itemToAnalyze = self.leftTextEdit.fileItemObject.getAnalyzeItem()
        if itemToAnalyze.numBasisPaths == 0:
            self.printToConsole("Basis paths have not been generated "
                                "for this file. Generating them now...")
            self.slotFindBasisPaths()
            self.funcQueue.append(self.slotShortestCases)
            return

        shortAnalyzer = GenericAnalyzer(1, self)
        
        if NumPathsDialog(shortAnalyzer, "Best",
            itemToAnalyze.projectConfig.OVER_COMPLETE_BASIS).exec_() == 1:
            for _ in range(len(itemToAnalyze.bestPaths)):
                path = itemToAnalyze.bestPaths[0]
                itemToAnalyze.removeChild(path)
                itemToAnalyze.fileList.removeItem(path)

            self.disableAnalysis()
            self.analysisThread.setAnalyzer(shortAnalyzer)
            self.analysisThread.setItem(itemToAnalyze)
            self.analysisThread.setFunc(self.bestCasesHelper,
                                        [shortAnalyzer])
            self.analysisThread.start()

    def bestCasesHelper(self, shortAnalyzer):
        """
        This is the function that is run in the new thread that handles running
        the analyzer to find best paths.
        """
        return shortAnalyzer.exec_()

    def slotLongestCases(self):
        """
        The action associated with accessing the menu bar item Run->Find Worst
        Paths. Begins generation of worst paths in a new thread if possible.
        This allows the user to interact with the GUI while analysis
        is running. The only files that can have worst paths are valid .c files
        that have been opened by the user.
        """
        if self.leftTextEdit.fileItemObject is None:
            self.printToConsole("There is currently no file for which GameTime "
                                "can generate worst-case feasible paths.")
            return

        itemToAnalyze = self.leftTextEdit.fileItemObject.getAnalyzeItem()
        if itemToAnalyze.numBasisPaths == 0:
            self.printToConsole("Basis paths have not been generated for "
                                "this file. Generating them now...")
            self.slotFindBasisPaths()
            self.funcQueue.append(self.slotLongestCases)
            return

        longAnalyzer = GenericAnalyzer(2, self)
        if NumPathsDialog(longAnalyzer, "Worst",
            itemToAnalyze.projectConfig.OVER_COMPLETE_BASIS).exec_() == 1:
            for _ in range(len(itemToAnalyze.worstPaths)):
                path = itemToAnalyze.worstPaths[0]
                itemToAnalyze.removeChild(path)
                itemToAnalyze.fileList.removeItem(path)
            self.disableAnalysis()
            self.analysisThread.setAnalyzer(longAnalyzer)
            self.analysisThread.setItem(itemToAnalyze)
            self.analysisThread.setFunc(self.worstCasesHelper,
                                        [longAnalyzer])
            self.analysisThread.start()

    def worstCasesHelper(self, longAnalyzer):
        """
        This is the function that is run in the new thread that handles running
        the analyzer to find worst paths.
        """
        return longAnalyzer.exec_()

    def slotGenerateHistogram(self):
        return HistogramDialog(self).exec_()

    def slotTogglePathHighlight(self):
        """
        The action associated with accessing the menu bar item View->Show
        Highlights. Highlights a displayed file, if possible.
        The only files that can be highlighted are those generated from
        executing GameTime; these are files that are derivatives of a
        parent file and that have Path objects associated with them.
        """
        self.highlightsEnabled = not self.highlightsEnabled
        fileSelect = self.fileSelectWidget.widget()
        if fileSelect.activeRight is not None:
            self.slotShowHighlights()

    def slotShowHighlights(self):
        """Highlight the currently selected path."""
        highlighter = Highlighter(self)
        return highlighter.exec_()

    def closeEvent(self, e):
        while self.tempFiles != set([]):
            temp = self.tempFiles.pop()
            os.remove(temp)
        super(GameTimeGui, self).closeEvent(e)

    def slotUpdateGui(self, path):
        path.addToMainWindow()
        self.addToWindow(path, Window.RIGHT)

    def slotFinishAnalysis(self):
        self.enableAnalysis()
        if len(self.funcQueue) > 0:
            nextFunc = self.funcQueue.pop()
            nextFunc()

    def slotProgress(self, currPathNum):
        self.printToConsole("Path %i has been computed" % currPathNum)

    def disableAnalysis(self):
        self.cancelAction.setDisabled(False)
        print "Disable analysis"
        for action in self.analysisActions:
            action.setDisabled(True)

    def enableAnalysis(self):
        self.cancelAction.setDisabled(True)
        print "enableAnalysis"
        for action in self.analysisActions:
            if self.generatedOvercompleteBasis and \
                not (action in self.overcompleteSupportedActions): continue
            action.setDisabled(False)

    def slotShowLoopDialog(self):
        if ConfirmationDialog("Loops in the code have been detected. "
                              "To analyze the code these loops must be "
                              "unrolled. Please specify bounds for each "
                              "loop.").exec_():
            loopDialog = LoopBoundsDialog(self)
            if loopDialog.exec_() == 0:
                self.enableAnalysis()
                self.funcQueue = []
                self.printToConsole("Current analysis was cancelled.")
            else:
                self.analysisThread.start()
        else:
            self.enableAnalysis()
            self.funcQueue = []
            self.printToConsole("Current analysis was cancelled.")

    def showMessageDialog(self, message, basis=False, title="Message"):
        if basis:
            BasisMessageDialog(message, self, title).exec_()
        else:
            MessageDialog(message, title).exec_()


class WorkerThreadSignals(QtCore.QObject):
    updateGui = Signal(FileItem)
    doneAnalyzing = Signal()
    showLoopDialog = Signal()
    showMessage = Signal(str, bool)
    printToConsole = Signal(str)
    showException = Signal(str, str)


class WorkerThread(QtCore.QThread):
    def __init__(self, gui, func, args=None):
        super(WorkerThread, self).__init__()
        self.gui = gui
        self.func = func
        self.args = args
        self.signals = WorkerThreadSignals()
        self.signals.showMessage.connect(self.gui.showMessageDialog)

    def run(self):
        if self.analyzer.enumCommand == 0:
            self.signals.printToConsole.emit("Generating basis paths...")
        elif self.analyzer.enumCommand == 1:
            self.signals.printToConsole.emit(
                "Generating %d best-case feasible path%s..." %
                (
                    self.analyzer.numPaths,
                    "s" if self.analyzer.numPaths > 1 else ""
                )
            )
        elif self.analyzer.enumCommand == 2:
            self.signals.printToConsole.emit(
                "Generating %d worst-case feasible path%s..." %
                (
                    self.analyzer.numPaths,
                    "s" if self.analyzer.numPaths > 1 else ""
                )
            )
        elif self.analyzer.enumCommand == 3:
            self.signals.printToConsole.emit(
                "Generating %d random feasible path%s..." %
                (
                    self.analyzer.numPaths,
                    "s" if self.analyzer.numPaths > 1 else ""
                )
            )
        elif self.analyzer.enumCommand == 4:
            self.signals.printToConsole.emit(
                "Generating all feasible paths in decreasing order of value..."
            )
        elif self.analyzer.enumCommand == 5:
            self.signals.printToConsole.emit(
                "Generating all feasible paths in increasing order of value..."
            )

        paths = self.func(*self.args)
        numPaths = len(paths)
        if numPaths > 0 or self.analyzer.enumCommand == 0:
            if paths == []:
                self.signals.printToConsole.emit(
                    "Loops were detected in the code."
                )
                self.signals.showLoopDialog.emit()
                return
            elif not paths:
                paths = []
            else:
                itemToAnalyze = self.itemToAnalyze
                if itemToAnalyze.preprocessedFileItem is None:
                    projectConfig = itemToAnalyze.projectConfig
                    preprocessedFile = projectConfig.locationTempFile
                    fileText = ""
                    with open(preprocessedFile) as preprocessedReader:
                        fileText = preprocessedReader.read()

                        preprocessedFileItem = FileItem(
                            " (Preprocessed)",
                            preprocessedFile,
                            fileText,
                            self.analyzer.mainWindow,
                            assign=True
                        )
                        preprocessedFileItem.originalName = \
                        itemToAnalyze.origLocation
                        preprocessedFileItem.setParent(self.itemToAnalyze)
                        preprocessedFileItem.setAnalyze(False)
                        itemToAnalyze.setPreprocessedFileItem(
                            preprocessedFileItem
                        )

                        self.signals.updateGui.connect(self.gui.slotUpdateGui)
                        self.signals.updateGui.emit(preprocessedFileItem)

                text = ""
                numPaths = self.analyzer.numPaths
                isBasis = False
                if self.analyzer.enumCommand == 0:
                    text = "Basis paths have been generated."
                    isBasis = True
                elif self.analyzer.enumCommand == 1:
                    text = ("%d best-case feasible path%s been generated." %
                            (numPaths, "s have" if numPaths > 1 else " has"))
                elif self.analyzer.enumCommand == 2:
                    text = ("%d worst-case feasible path%s been generated." %
                            (numPaths, "s have" if numPaths > 1 else " has"))
                elif self.analyzer.enumCommand == 3:
                    text = ("%d random feasible path%s been generated." %
                            (numPaths, "s have" if numPaths > 1 else " has"))
                elif self.analyzer.enumCommand == 4:
                    text = ("All feasible paths have been generated in "
                            "decreasing order of value.")
                elif self.analyzer.enumCommand == 5:
                    text = ("All feasible paths have been generated in "
                            "increasing order of value.")
                self.signals.printToConsole.emit(text)
                self.signals.showMessage.emit(text, isBasis)

        caseNumber = 1
        toWrite = []
        if self.analyzer.enumCommand == 0:
            self.itemToAnalyze.numBasisPaths = 0
            pathList = self.itemToAnalyze.basisPaths
            label = "+ Basis Path "
        elif self.analyzer.enumCommand == 1:
            pathList = self.itemToAnalyze.bestPaths
            label = "+ Best Path "
        elif self.analyzer.enumCommand == 2:
            pathList = self.itemToAnalyze.worstPaths
            label = "+ Worst Path "
        elif self.analyzer.enumCommand == 3:
            pathList = self.itemToAnalyze.randomPaths
            label = "+ Random Path "
        elif self.analyzer.enumCommand == 4 or self.analyzer.enumCommand == 5:
            pathList = self.itemToAnalyze.allPaths
            label = "+ Path "

        for path in paths:
            if ((caseNumber > self.analyzer.numPaths and
                 self.analyzer.enumCommand in [1, 2, 3])):
                break
            if self.analyzer.enumCommand == 0:
                self.itemToAnalyze.numBasisPaths += 1
                toWrite.append(path)

            caseData = ("Assignments:\n%s\n\nPredicted Value:\n%s\n\n"
                        "Measured Value:\n%s" %
                        (path.getAssignments(),
                         path.getPredictedValue(),
                         path.getMeasuredValue()))

            preprocessedFileItem = self.itemToAnalyze.preprocessedFileItem
            pathItem = FileItem(
                "%s%d" % (label, caseNumber),
                preprocessedFileItem.origLocation,
                caseData,
                self.analyzer.mainWindow,
                assign=True
            )
            pathItem.originalName = self.itemToAnalyze.origLocation
            pathItem.setParent(self.itemToAnalyze)
            pathList.append(pathItem)
            pathItem.setHighlightPath(path)
            pathItem.setAnalyze(False)

            # self.signals.progress.emit(caseNumber)
            caseNumber += 1
            self.signals.updateGui.connect(self.gui.slotUpdateGui)
            self.signals.updateGui.emit(pathItem)
        # if self.analyzer.enumCommand == 0:
            # self.itemToAnalyze.analyzer.writeBasisPathsToFiles(toWrite)
        self.signals.doneAnalyzing.emit()

    def setFunc(self, func, args=None):
        self.func = func
        self.args = args or []

    def setAnalyzer(self, analyzer):
        self.analyzer = analyzer

    def setItem(self, item):
        self.itemToAnalyze = item


def showMainWindow():
    """Creates the application for the GUI and shows the main window."""
    from gametime.defaults import logger
    logger.info("Starting up the GameTime GUI...")

    # One application created for the GUI.
    # TODO (jokotker): What is happening here?
    # Maintains the main window.
    app_gametime = QtGui.QApplication(sys.argv)

    # Start an instance of the GUI.
    gui_instance = GameTimeGui()
    gui_instance.show()

    # Execute the application.
    sys.exit(app_gametime.exec_())
    logger.info("GameTime GUI closed.")

def startGui():
    # Construct the location of the directory that contains
    # the batch file that prepares and starts
    # the GameTime graphical user interface.
    from gametime.defaults import sourceDir
    guiInitBatchFile = os.path.join(sourceDir,
                                    os.path.join("bin", "gametime-gui.bat"))
    subprocess.call([guiInitBatchFile], shell=True)


if __name__ == "__main__":
    showMainWindow()
