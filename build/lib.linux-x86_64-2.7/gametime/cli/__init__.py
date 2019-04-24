#!/usr/bin/env python

"""Conducts a series of operations to initialize
the command-line interface for the GameTime module.
"""

"""See the LICENSE file, located in the root directory of
the source distribution and
at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
for details on the GameTime license and authors.
"""


import os
import subprocess
import sys
import webbrowser

from gametime.defaults import config, logger, sourceDir
from gametime.updateChecker import isUpdateAvailable


def startCli():
    """Prepares and starts the command-line interface to GameTime."""
    logger.info("Welcome to GameTime!")
    logger.info("")

    logger.info("Checking for any available updates...")
    updateAvailable, latestVersionInfo = isUpdateAvailable()
    if updateAvailable:
        version = latestVersionInfo["version"]
        infoUrl = latestVersionInfo["info_url"]
        logger.info("An updated version of GameTime (%s) is available. "
                    "The current version is %s." % (version, config.VERSION))

        choice = raw_input("Would you like to download and install "
                           "this version? Please enter `[Y]es` "
                           "or `[N]o`: ").lower()
        while choice not in ["y", "yes", "n", "no"]:
            choice = raw_input("Please enter `[Y]es` or `[N]o`: ").lower()
        if choice in ["y", "yes"]:
            logger.info("Exiting GameTime...")
            try:
                webbrowser.open(infoUrl)
                sys.exit(0)
            except webbrowser.Error as e:
                logger.warning("Unable to open a web browser to display "
                               "information about the updated version: %s " % e)
                logger.warning("Please visit %s to download and install "
                               "the updated version." % infoUrl)
        else:
            logger.info("Update not installed.")
            logger.info("Please visit %s to download and install "
                        "an updated version of GameTime." % infoUrl)
    elif not latestVersionInfo:
        logger.warning("Unable to obtain information about available updates.")
        logger.warning("Please check %s for the latest version of GameTime "
                       "and for available updates." % config.WEBSITE_URL)
    else:
        logger.warning("No updates to GameTime are available.")
    logger.info("")

    # Construct the location of the directory that contains
    # the batch file that initializes the GameTime command-line interface.
    cliInitBatchFile = os.path.join(sourceDir,
                                    os.path.join("bin", "gametime-cli.bat"))
    subprocess.call([cliInitBatchFile], shell=True)
