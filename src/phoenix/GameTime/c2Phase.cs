/*************************************************************************************************
 * c2Phase                                                                                       *
 * ============================================================================================= *
 * This file contains the classes that define and register the GameTime                          *
 * analysis as one of the Microsoft Phoenix components.                                          *
 *                                                                                               *
 * See LICENSE for full license details (BSD license).                                           *
 *************************************************************************************************/

using Phx;

using System;
using System.Collections;

using Utilities;


namespace GameTime
{
    /// <summary>
    /// This class defines the GameTime analysis pass.
    /// </summary>
    public class GameTimePass : Phx.Passes.Pass
    {
        /// <summary>
        /// The GameTime component control.
        /// </summary>
        public static Phx.Controls.ComponentControl gameTimeControl;

        /// <summary>
        /// Name that corresponds to the GameTime analysis pass.
        /// </summary>
        static string passName = @"GameTimeAnalysisPass";

        /// <summary>
        /// Enumeration that denotes the possible modes for GameTimeAnalysis pass.
        /// </summary>
        public enum MODES
        {
            /// <summary>
            /// Create the DAG that contains the basic blocks of a function unit.
            /// </summary>
            CREATE_DAG,

            /// <summary>
            /// Find the conditions along a basis path.
            /// </summary>
            FIND_CONDITIONS
        };

        /// <summary>
        /// Represents the current mode of GameTimeAnalysisPass.
        /// </summary>
        public static MODES mode = MODES.CREATE_DAG;

        /// <summary>
        /// New creates an instance of a pass. Following Phoenix guidelines, New is static.
        /// </summary>
        /// 
        /// <param name="config">Pointer to a Passes::PassConfiguration that provides
        /// properties for retrieving the initial pass list.</param>
        /// <returns>A pointer to the new pass.</returns>
        static public GameTimePass New(Phx.Passes.PassConfiguration config)
        {
            GameTimePass pass = new GameTimePass();
            Phx.Graphs.CallGraphProcessOrder order = Phx.Graphs.CallGraphProcessOrder.BottomUp;
            pass.Initialize(config, order, passName);

            /* Set its pass control. */
            pass.PassControl = GameTimePass.gameTimeControl;

            /* Build the PhaseList with only 2 phases: C2::Phases::CxxILReaderPhase and Phase.
             * You can add more phases to the new pass here. */
            Phx.Phases.PhaseConfiguration phaseConfiguration =
               Phx.Phases.PhaseConfiguration.New(pass.Configuration.Lifetime, "GameTime Phase");
            
            Phx.Phases.PhaseList phaseList =
                C2.Phases.Builder.BuildPreCompilePhaseList(phaseConfiguration);
            phaseConfiguration.PhaseList.AppendPhase(phaseList);

            pass.PhaseConfigurationNative = phaseConfiguration;
            pass.PhaseConfiguration = phaseConfiguration;
            
            return pass;
        }

        /// <summary>
        /// Execute is the pass's prime mover; all unit-centric processing occurs here. Note that
        /// Execute might be thought of as a "callback": as the C2 host compiles each FunctionUnit,
        /// passing it from pass to pass, the plug-in Execute method is called to do its work.
        /// </summary>
        /// 
        /// <param name="moduleUnit">The moduleUnit to process.</param>
        /// <returns>True if successful; false otherwise.</returns>
        protected override bool Execute(Phx.ModuleUnit moduleUnit)
        {
            try
            {
                this.ExecuteHelper(moduleUnit);
            }
            catch (System.Exception ex)
            {
                Console.Out.WriteLine("PHOENIX: Exception occurred:");
                Console.Out.WriteLine(ex.Message);
                Console.Out.WriteLine("PHOENIX: Stack trace:");
                Console.Out.WriteLine(ex.StackTrace);
                Console.Out.WriteLine("PHOENIX: Exiting GameTime...");
                Environment.Exit(1);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Private helper workhorse function for <see cref="Execute"/>.
        /// </summary>
        /// 
        /// <param name="moduleUnit">The moduleUnit to process.</param>
        /// <returns>True if successful; false otherwise.</returns>
        private bool ExecuteHelper(Phx.ModuleUnit moduleUnit)
        {
            Console.Out.WriteLine();

            Phx.Graphs.CallGraph callGraph = moduleUnit.CallGraph;
            if (callGraph == null)
            {
                Console.Out.WriteLine("PHOENIX: Call graph is null.");
                Console.Out.WriteLine("Exiting GameTime.");
                Environment.Exit(1);
                return false;
            }

            /* Load GameTime configuration. */
            string configFile = Console.ReadLine();
            Console.Out.WriteLine("PHOENIX: Configuring this GameTime session with " +
                configFile + "...");
            Utilities.Configuration config = Utilities.Configuration.ReadConfigFile(configFile);
            Console.Out.WriteLine("PHOENIX: Successfully configured this session.");

            /* Load the project configuration. */
            string projectConfigFile = Console.ReadLine();
            Console.Out.WriteLine("PHOENIX: Loading project configuration from " +
                projectConfigFile + "...");
            ProjectConfiguration projectConfig =
                ProjectConfiguration.ReadProjectConfigFile(projectConfigFile, config);
            Console.Out.WriteLine("PHOENIX: Successfully loaded the project for this session.");

            /* Determine the current GameTime operation mode. */
            string currentMode = Console.ReadLine();
            mode = (currentMode.Equals(config.TEMP_PHX_CREATE_DAG)) ?
                MODES.CREATE_DAG : MODES.FIND_CONDITIONS;
            Console.Out.WriteLine("PHOENIX: GameTime operation mode is: " +
                ((mode == MODES.CREATE_DAG) ? "Create DAG." : "Find path conditions."));

            /* Find the function to analyze. */
            string funcToProcess = projectConfig.func;

            Console.Out.WriteLine("PHOENIX: Preprocessing " + funcToProcess + "...");
            Console.Out.WriteLine();

            Console.Out.WriteLine("PHOENIX: Finding the corresponding function unit...");

            /* Find the function unit corresponding to the function to be analyzed. */
            FunctionUnit functionUnitToProcess = null;

            /* Traverse the graph in post-order (top-down order). */
            Phx.Graphs.NodeFlowOrder nodeOrder = Phx.Graphs.NodeFlowOrder.New(callGraph.Lifetime);
            nodeOrder.Build(callGraph, Phx.Graphs.Order.PostOrder);
            Phx.Targets.Runtimes.Runtime runtime = moduleUnit.Runtime;
            uint functionCount = 0;
            for (uint i = 1; i <= nodeOrder.NodeCount; ++i)
            {
                Phx.Graphs.CallNode node = nodeOrder.Node(i).AsCallNode;
                if ((node == callGraph.UnknownCallerNode) ||
                    (node == callGraph.UnknownCalleeNode))
                {
                    continue;
                }
                if (node.FunctionSymbol != null)
                {
                    /* Is this LTCG mode? */
                    bool isLTCG = false;
                    try
                    {
                        IDictionary env = Environment.GetEnvironmentVariables();
                        if (env.Contains("LINK_TIME_CODE_GENERATION")) { isLTCG = true; }
                    }
                    catch (ArgumentNullException) { }

                    /* Only perform the check when the LTCG mode is off. */
                    if (isLTCG || moduleUnit.IsPEModuleUnit)
                    {
                        moduleUnit = node.FunctionSymbol.CompilationUnitParentSymbol.Unit.AsModuleUnit;
                    }

                    /* Create the corresponding function unit. */
                    Phx.Lifetime lifetime = Phx.Lifetime.New(Phx.LifetimeKind.Function, null);
                    Phx.FunctionUnit functionUnit = Phx.FunctionUnit.New(lifetime,
                        node.FunctionSymbol, Phx.CodeGenerationMode.Native,
                        moduleUnit.TypeTable, runtime.Architecture, runtime,
                        moduleUnit, functionCount++);

                    /* Attach debugging info. */
                    Phx.Debug.Info.New(functionUnit.Lifetime, functionUnit);

                    node.FunctionSymbol.FunctionUnit = functionUnit;
                    this.PhaseConfiguration.PhaseList.DoPhaseList(functionUnit);
                    functionUnit.Context.PopUnit();

                    string funcName = FunctionUnitHelper.GetFunctionName(functionUnit);
                    if (funcName == funcToProcess)
                    {
                        functionUnitToProcess = functionUnit;
                        break;
                    }
                }
            }

            if (functionUnitToProcess == null)
            {
                Console.Out.WriteLine("PHOENIX: Cannot find function named " + funcToProcess + ".");
                Console.Out.WriteLine("PHOENIX: Exiting GameTime...");
                Environment.Exit(1);
                return false;
            }
            else
            {
                Console.Out.WriteLine("PHOENIX: Function unit found.");
            }

            Console.Out.WriteLine("PHOENIX: Preprocessing the function unit...");
            FunctionUnitHelper.Preprocess(functionUnitToProcess);
            Console.Out.WriteLine("PHOENIX: Function unit preprocessing complete.");
            Console.Out.WriteLine();

            Console.Out.WriteLine("PHOENIX: Building the flow graph...");
            functionUnitToProcess.BuildFlowGraphWithoutEH();
            Phx.Graphs.FlowGraph graph = functionUnitToProcess.FlowGraph;
            Console.Out.WriteLine("PHOENIX: Flow graph built.");

            Console.Out.WriteLine("PHOENIX: Snipping the relevant portion of the flow graph...");

            uint sourceBlockId = 1;
            uint sinkBlockId = graph.NodeCount;

            if (projectConfig.startLabel != "")
            {
                Phx.Graphs.BasicBlock sourceBlock =
                    FunctionUnitHelper.SplitAtUserLabel(functionUnitToProcess,
                        projectConfig.startLabel);
                sourceBlockId = sourceBlock.Id;
            }
            if (projectConfig.endLabel != "")
            {
                Phx.Graphs.BasicBlock sinkBlock =
                    FunctionUnitHelper.SplitAtUserLabel(functionUnitToProcess,
                        projectConfig.endLabel);
                /* Correct the sink block: we want the block just before the block
                 * we receive from SplitAtUserLabel. */
                Phx.Graphs.FlowEdge edgeToSink = sinkBlock.PredecessorEdgeList;
                sinkBlock = edgeToSink.PredecessorNode;
                sinkBlockId = sinkBlock.Id;
            }

            Console.Out.WriteLine("PHOENIX: Relevant portion snipped.");
            Console.Out.WriteLine();

            Console.Out.WriteLine("PHOENIX: Starting analysis...");

            switch (mode)
            {
                case MODES.CREATE_DAG:
                    FunctionUnitHelper.DumpCfgToFile(functionUnitToProcess,
                        sourceBlockId, sinkBlockId, config, projectConfig);
                    break;
                case MODES.FIND_CONDITIONS:
                    FunctionUnitHelper.FindPathConditions(functionUnitToProcess,
                        config, projectConfig);
                    break;
            }

            Console.Out.WriteLine("PHOENIX: Analysis successful.");
            Console.Out.WriteLine();

            Environment.Exit(0);
            return true;
        }
    }
    
    /// <summary>
    /// This class defines the GameTime analysis phase.
    /// </summary>
    public class GameTimePhase : Phx.Phases.Phase
    {
        /// <summary>
        /// New creates an instance of a phase. Following Phoenix guidelines, New is static.
        /// </summary>
        /// 
        /// <param name="config">A pointer to a Phases.PhaseConfiguration that
        /// provides properties for retrieving the initial phase list.</param>
        /// <returns>A pointer to the new phase.</returns>
        static public GameTimePhase New(Phx.Phases.PhaseConfiguration config)
        {
            GameTimePhase phase = new GameTimePhase();
            phase.Initialize(config, "GameTime plugin");
            phase.PhaseControl = null;
            return phase;
        }
     
        /// <summary>
        /// Execute is the phase's prime mover; all unit-centric processing occurs here. Note that
        /// Execute might be thought of as a "callback": as the C2 host compiles each FunctionUnit,
        /// passing it from phase to phase, the plug-in Execute method is called to do its work.
        /// </summary>
        /// 
        /// <remarks>Since the IR exists only at the FunctionUnit level, we ignore ModuleUnits.
        /// The order of units in a compiland passed to Execute is indeterminate.</remarks>
        /// 
        /// <param name="unit">The unit to process.</param>
        protected override void Execute(Phx.Unit unit)
        {
        }
    }

    /// <summary>
    /// This class defines the GameTime-specific plugin for the C2 backend.
    /// </summary>
    public class GameTimePlugin : Phx.PlugIn
    {
        /// <summary>
        /// RegisterObjects initializes the plug-in's environment. Normally, this includes defining
        /// your command-line switches (controls) that should be handled by Phoenix. Phoenix calls
        /// this method early, upon loading the plug-in's DLL.
        /// </summary>
        /// 
        /// <remarks>
        /// The RegisterObjects method is not the place to deal with phase-specific issues, because
        /// the host has not yet built its phase list. However, controls ARE phase-specific.
        /// Because the phase object does not exist yet, the phase's controls must be static
        /// fields, accessible from here.
        /// </remarks>
        public override void RegisterObjects()
        {
        }

        /// <summary>
        /// BuildPhases is where the plug-in creates and initializes its phase object(s), and
        /// inserts them into the phase list already created by the c2 host.
        /// </summary>
        /// 
        /// <remarks>
        /// This is where the plug-in determines a new phase's place in the list of C2 CodeGen
        /// pass by locating an existing phase by name and inserting the new phase
        /// before or after it.
        /// </remarks>
        /// 
        /// <param name="config">Pointer to a Phases.PhaseConfiguration of C2 CodeGen Pass.</param>
        public override void BuildPhases(Phx.Phases.PhaseConfiguration config)
        {
            /* You still can add/replace a phase in C2 CodeGen Pass (not the new pass). */
        }

        /// <summary>
        /// BuildPasses is where the plug-in creates and initializes its pass object(s), and
        /// inserts them into the pass list already created by the c2 host.
        /// </summary>
        public override void BuildPasses(Phx.Passes.PassConfiguration passConfiguration)
        {
            Phx.Passes.Pass basePass = passConfiguration.PassList.FindByName("C2 Pass 0");
            if (basePass == null)
            {
                Phx.Output.WriteLine("C2 Pass 0 not found in passlist:");
                Phx.Output.Write(passConfiguration.ToString());
                return;
            }

            GameTimePass newPass = GameTimePass.New(passConfiguration);
            basePass.InsertAfter(newPass);
        }

        /// <summary>
        /// Returns the name of this plugin.
        /// </summary>
        /// 
        /// <returns>Name of this plugin.</returns>
        public override String NameString
        {
            get { return "GameTime-plugin"; }
        }
    }
}
