#!/usr/bin/env python

"""Conducts a series of operations to initialize the GameTime module,
including the definition of a static class that allows a user
to interact with GameTime through Python scripts.
"""

"""See the LICENSE file, located in the root directory of
the source distribution and
at http://verifun.eecs.berkeley.edu/gametime/about/LICENSE,
for details on the GameTime license and authors.
"""


import analyzer
import cilHelper
import configuration
import defaults
import fileHelper
import gametimeError
import indexExpression
import inliner
import loopHandler
import merger
import nxHelper
import path
import phoenixHelper
import projectConfiguration
import pulpHelper
import simulators
import smt
import updateChecker

from analyzer import Analyzer
from defaults import logger
from gametimeError import GameTimeError
from pathGenerator import PathType


class GameTime(object):
    """Contains methods and variables that allow a user to import
    GameTime as a module.
    """
    @staticmethod
    def analyze(projectConfig):
        """
        Arguments:
            projectConfig:
                :class:`~gametime.projectConfiguration.ProjectConfiguration`
                object that represents the configuration of a GameTime project.

        Returns:
            :class:`~gametime.analyzer.Analyzer` object for the project
            configuration provided.
        """
        try:
            analyzer = Analyzer(projectConfig)
            analyzer.createDag()
            return analyzer
        except GameTimeError as e:
            logger.error(str(e))
            raise e
