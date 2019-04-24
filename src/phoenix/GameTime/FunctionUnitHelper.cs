/*************************************************************************************************
 * FunctionUnitHelper                                                                            *
 * ============================================================================================= *
 * This file contains the class that defines various helper functions                            *
 * to interact with a Phoenix function unit.                                                     *
 *                                                                                               *
 * See LICENSE for full license details (BSD license).                                           *
 *************************************************************************************************/

using System;
using System.Collections.Generic;
using System.IO;

using Phx.SSA;

using Utilities;


namespace GameTime  
{
    /// <summary>
    /// This class defines various helper functions to interact with a Phoenix function unit.
    /// </summary>
    public class FunctionUnitHelper
    {
        /// <summary>
        /// Preprocesses the function unit provided.
        /// </summary>
        /// 
        /// <param name="functionUnit">Phoenix function unit to preprocess.</param>
        public static void Preprocess(Phx.FunctionUnit functionUnit)
        {
            Console.Out.WriteLine("PHOENIX: Building SSA information...");
            functionUnit.BuildSsaInfo(BuildOptions.DefaultNotAliased);
            Console.Out.WriteLine("PHOENIX: SSA information built.");
        }

        /// <summary>
        /// Returns the name of the function that corresponds to the function unit provided.
        /// </summary>
        /// 
        /// <param name="functionUnit">Phoenix function unit whose name is to be returned.</param>
        /// <returns>Name of the Phoenix function unit.</returns>
        public static string GetFunctionName(Phx.FunctionUnit functionUnit)
        {
            string funcName = Phx.Utility.Undecorate(functionUnit.NameString, true);
            if (funcName.StartsWith("_")) { funcName = funcName.Substring(1); }
            return funcName;
        }

        /// <summary>
        /// Dumps the DOT representation of the control-flow graph of the input function unit
        /// in a file (a DAG file labeled with the name of the function unit) that will be
        /// further processed by the GameTime Python scripts.
        /// </summary>
        /// 
        /// <param name="functionUnit">Phoenix function unit whose control-flow graph is
        /// to be dumped.</param>
        /// <param name="sourceNodeId">ID of the BasicBlock in the control-flow
        /// graph to start the analysis from.</param>
        /// <param name="sinkNodeId">ID of the BasicBlock in the control-flow
        /// graph to end the analysis at.</param>
        /// <param name="config">Configuration object that contains
        /// GameTime configuration information.</param>
        /// <param name="projectConfig">ProjectConfiguration object that contains
        /// project configuration information.</param>
        public static void DumpCfgToFile(Phx.FunctionUnit functionUnit,
            uint sourceNodeId, uint sinkNodeId, Utilities.Configuration config,
            ProjectConfiguration projectConfig)
        {
            Console.Out.WriteLine("PHOENIX: Dumping a DOT representation of the " +
                "control-flow graph...");

            string funcName = GetFunctionName(functionUnit);
            Phx.Graphs.FlowGraph flowGraph = functionUnit.FlowGraph;
            Phx.Lifetime lifetime = functionUnit.Lifetime;

            if (projectConfig.debugConfig.DUMP_IR)
            {
                Console.Out.WriteLine("PHOENIX: Dumping IR...");

                string irFileName =
                    System.IO.Path.Combine(projectConfig.locationTempDir,
                    config.TEMP_PHX_IR);
                StreamWriter irWriter = new StreamWriter(irFileName, true);
                irWriter.AutoFlush = true;

                foreach (Phx.Graphs.BasicBlock block in flowGraph.BasicBlocks)
                {
                    irWriter.WriteLine("*** BASIC BLOCK " + block.Id + " ***");
                    foreach (Phx.IR.Instruction instruction in block.Instructions)
                    {
                        irWriter.WriteLine(instruction);
                    }
                }

                irWriter.Close();
            }


            /* Perform a reachability analysis from the source node: determine
             * what nodes and edges can be reached from the source node. */
            Phx.BitVector.Sparse blocksVisitedFromSource = Phx.BitVector.Sparse.New(lifetime);
            Phx.BitVector.Sparse blocksToVisitFromSource = Phx.BitVector.Sparse.New(lifetime);
            Phx.BitVector.Sparse edgesVisitedFromSource = Phx.BitVector.Sparse.New(lifetime);

            blocksToVisitFromSource.SetBit(sourceNodeId);
            /* TODO: Make more efficient. */
            while (!blocksToVisitFromSource.IsEmpty)
            {
                uint blockToVisitId = blocksToVisitFromSource.RemoveFirstBit();
                blocksVisitedFromSource.SetBit(blockToVisitId);

                Phx.Graphs.BasicBlock blockToVisit =
                    flowGraph.Node(blockToVisitId) as Phx.Graphs.BasicBlock;
                foreach (Phx.Graphs.FlowEdge edge in blockToVisit.SuccessorEdges)
                {
                    Phx.Graphs.BasicBlock successor = edge.SuccessorNode;

                    uint succId = successor.Id;
                    uint edgeId = edge.Id;

                    if (!blocksVisitedFromSource.GetBit(succId))
                    {
                        blocksToVisitFromSource.SetBit(succId);
                        edgesVisitedFromSource.SetBit(edgeId);
                    }
                }
            }

            /* Perform a backward reachability analysis from the sink node: determine
             * what nodes and edges can be reached from the sink node. */
            Phx.BitVector.Sparse blocksVisitedFromSink = Phx.BitVector.Sparse.New(lifetime);
            Phx.BitVector.Sparse blocksToVisitFromSink = Phx.BitVector.Sparse.New(lifetime);
            Phx.BitVector.Sparse edgesVisitedFromSink = Phx.BitVector.Sparse.New(lifetime);

            blocksToVisitFromSink.SetBit(sinkNodeId);

            while (!blocksToVisitFromSink.IsEmpty)
            {
                uint blockToVisitId = blocksToVisitFromSink.RemoveFirstBit();
                blocksVisitedFromSink.SetBit(blockToVisitId);

                Phx.Graphs.BasicBlock blockToVisit =
                    flowGraph.Node(blockToVisitId) as Phx.Graphs.BasicBlock;
                foreach (Phx.Graphs.FlowEdge edge in blockToVisit.PredecessorEdges)
                {
                    Phx.Graphs.BasicBlock predecessor = edge.PredecessorNode;

                    uint predId = predecessor.Id;
                    uint edgeId = edge.Id;

                    if (!blocksVisitedFromSink.GetBit(predId))
                    {
                        blocksToVisitFromSink.SetBit(predId);
                        edgesVisitedFromSink.SetBit(edgeId);
                    }
                }
            }

            /* Determine which blocks and edges in the control-flow graph can be reached from
             * *both* the source and the sink: this is the section of the control-flow graph
             * that we are interested in. */
            blocksVisitedFromSource.And(blocksVisitedFromSink);
            edgesVisitedFromSource.And(edgesVisitedFromSink);

            uint numNodes = (uint) blocksVisitedFromSource.GetBitCount();
            uint numEdges = (uint) edgesVisitedFromSource.GetBitCount();

            uint numNewNodes = numNodes + numNodes;
            uint numNewEdges = numEdges + numNodes + 1;

            /* Mapping between old block IDs and new block IDs. */
            Dictionary<uint, uint> idMap = new Dictionary<uint, uint>();

            /* Create a writer to write the DAG in DOT format to a file. */
            string dagFileName =
                System.IO.Path.Combine(projectConfig.locationTempDir, config.TEMP_DAG);
            StreamWriter dagWriter = new StreamWriter(dagFileName);
            dagWriter.AutoFlush = true;

            /* Qualify the DAG. */
            dagWriter.WriteLine("digraph " + funcName + " {");

            uint newMappedBlockId = 1;
            uint newFakeBlockId = numNodes + 1;
            foreach (Phx.Graphs.BasicBlock basicBlock in flowGraph.BasicBlocks)
            {
                uint blockId = basicBlock.Id;

                /* Is the block in the portion of the control-flow graph
                 * we are concerned about? */
                if (blocksVisitedFromSource.GetBit(blockId))
                {
                    uint mappedBlockId;
                    if (idMap.ContainsKey(blockId))
                    {
                        mappedBlockId = idMap[blockId];
                    }
                    else
                    {
                        idMap.Add(blockId, newMappedBlockId);
                        mappedBlockId = newMappedBlockId++;
                    }

                    /* Keep track of the edges that may be added to the resulting DAG. */
                    List<Pair<uint, uint>> edges = new List<Pair<uint, uint>>();

                    /* First edge that may be added to the DAG: it will be added
                     * if there are successors. */
                    edges.Add(new Pair<uint, uint>(mappedBlockId, newFakeBlockId));

                    List<Phx.Graphs.FlowEdge> reverseSuccessorList = new List<Phx.Graphs.FlowEdge>();
                    foreach (Phx.Graphs.FlowEdge edge in basicBlock.SuccessorEdges)
                    {
                        reverseSuccessorList.Add(edge);
                    }
                    reverseSuccessorList.Reverse();

                    foreach (Phx.Graphs.FlowEdge edge in reverseSuccessorList)
                    {
                        /* Is the block in the portion of the control-flow graph
                         * we are concerned about? */
                        Phx.Graphs.BasicBlock successor = edge.SuccessorNode;
                        uint succId = successor.Id;

                        if (blocksVisitedFromSource.GetBit(succId))
                        {
                            uint mappedSuccId;
                            if (idMap.ContainsKey(succId))
                            {
                                mappedSuccId = idMap[succId];
                            }
                            else
                            {
                                idMap.Add(succId, newMappedBlockId);
                                mappedSuccId = newMappedBlockId++;
                            }

                            /* Replace each BasicBlock with two edges,
                             * separated by a new, "fake" node. */
                            edges.Add(new Pair<uint, uint>(newFakeBlockId, mappedSuccId));
                        }
                    }

                    if (edges.Count > 1)
                    {
                        /* This node has successors in the "snipped" CFG. */
                        foreach (Pair<uint, uint> edge in edges)
                        {
                            dagWriter.WriteLine("  " + edge.First + " -> " + edge.Second + ";");
                        }

                        /* Generate the ID of a "fake" node corresponding to
                         * the next node, if any. */
                        newFakeBlockId++;
                    }
                }
            }

            /* Add a "dummy" edge from the sink. */
            dagWriter.WriteLine("  " + idMap[sinkNodeId] + " -> " + numNewNodes + ";");

            dagWriter.WriteLine("}");
            dagWriter.Close();

            /* Copy the mappings to a file. */
            string blockIdMapFile =
                System.IO.Path.Combine(projectConfig.locationTempDir, config.TEMP_DAG_ID_MAP);
            StreamWriter blockIdMapWriter = new StreamWriter(blockIdMapFile);
            blockIdMapWriter.AutoFlush = true;

            foreach (uint key in idMap.Keys)
            {
                uint blockVisitedId = key;
                uint newBlockId = idMap[key];

                blockIdMapWriter.WriteLine(newBlockId + " " + blockVisitedId);
            }

            blockIdMapWriter.Close();
        }

        /// <summary>
        /// Finds the conditions along a path.
        /// </summary>
        /// 
        /// <param name="functionUnit">Phoenix function unit.</param>
        /// <param name="config">Configuration object that contains GameTime
        /// configuration information.</param>
        /// <param name="projectConfig">ProjectConfiguration object that
        /// contains project configuration information.</param>
        public static void FindPathConditions(Phx.FunctionUnit functionUnit,
            Utilities.Configuration config, ProjectConfiguration projectConfig)
        {
            Console.Out.WriteLine("PHOENIX: Finding the conditions and " +
                "assignments along a path...");

            Console.Out.WriteLine("PHOENIX: Reading in the DAG...");

            /* Read in the DAG and deduce associated information. The DAG is in DOT file format. */
            string dagFileName =
                System.IO.Path.Combine(projectConfig.locationTempDir, config.TEMP_DAG);
            TextReader dagReader = new StreamReader(dagFileName);

            /* Ignore the first line. */
            dagReader.ReadLine();
            /* Read in the nodes and the edges of the graph. */
            HashSet<string> dagNodes = new HashSet<string>();
            HashSet<Pair<string, string>> dagEdges = new HashSet<Pair<string, string>>();
            string dagEdge = null;
            while ((dagEdge = dagReader.ReadLine()) != null)
            {
                dagEdge = dagEdge.Replace("{", "");
                dagEdge = dagEdge.Replace("}", "");
                dagEdge.Trim();
                if (dagEdge.Length != 0)
                {
                    /* Ignore the semicolon. */
                    dagEdge = dagEdge.Trim(';');
                    /* Split at the arrow. */
                    dagEdge = dagEdge.Replace("->", ">");
                    string[] edgeNodes = dagEdge.Split('>');
                    /* Find the nodes incident to the edge. */
                    edgeNodes[0] = edgeNodes[0].Trim();
                    edgeNodes[1] = edgeNodes[1].Trim();
                    /* Add the nodes and the edge. */
                    dagNodes.Add(edgeNodes[0]);
                    dagNodes.Add(edgeNodes[1]);
                    dagEdges.Add(new Pair<string, string>(edgeNodes[0], edgeNodes[1]));
                }
            }

            dagReader.Close();

            uint numNodes = (uint) dagNodes.Count;
            uint numEdges = (uint) dagEdges.Count;
            Console.Out.WriteLine("PHOENIX: DAG has " + numNodes + " nodes and " +
                numEdges + " edges.");
            Console.Out.WriteLine("PHOENIX: There are at most " +
                (numEdges - numNodes + 2) + " basis paths.");

            /* How many nodes are there in the original DAG, without the fake
             * intermediate nodes? */
            uint actualNumNodes = (numNodes / 2);
            uint actualNumEdges = (numEdges - actualNumNodes - 1);

            /* Get the successors of the nodes that existed in the
             * original DAG (without the fake intermediate nodes). */
            Dictionary<uint, uint> nodeSuccessors = new Dictionary<uint, uint>();
            foreach (Pair<string, string> edge in dagEdges)
            {
                uint sourceNodeId = Convert.ToUInt32(edge.First);
                uint sinkNodeId = Convert.ToUInt32(edge.Second);

                /* Yes, there can be more than one successor for any given node.
                 * However, we are guaranteed that for the nodes in the original
                 * DAG, there is exactly one (fake) successor node. We are only
                 * concerned about these nodes. There is no great way to programatically
                 * determine these successor nodes. */
                if (sourceNodeId <= actualNumNodes)
                {
                    nodeSuccessors.Add(sourceNodeId, sinkNodeId);
                }
            }

            Console.Out.WriteLine("PHOENIX: Finished reading and processing the DAG.");

            Console.Out.WriteLine("PHOENIX: Reading the block ID map...");

            /* Read and store the mapping between "adjusted" block IDs and "actual"
             * block IDs. Also, store the reverse mapping for quick conversion
             * in the other direction. */
            Dictionary<uint, uint> adjustedToActual = new Dictionary<uint, uint>();
            Dictionary<uint, uint> actualToAdjusted = new Dictionary<uint, uint>();

            string idMapFileName =
                System.IO.Path.Combine(projectConfig.locationTempDir, config.TEMP_DAG_ID_MAP);
            string[] idMappings = File.ReadAllLines(idMapFileName);
            foreach (string idMapping in idMappings)
            {
                string[] idMappingArray = idMapping.Split(' ');

                uint adjustedBlockId = Convert.ToUInt32(idMappingArray[0]);
                uint actualBlockId = Convert.ToUInt32(idMappingArray[1]);

                adjustedToActual.Add(adjustedBlockId, actualBlockId);
                actualToAdjusted.Add(actualBlockId, adjustedBlockId);
            }

            Console.Out.WriteLine("PHOENIX: Finished reading and processing the ID map.");
            Console.Out.WriteLine();

            Console.Out.WriteLine("PHOENIX: Reading in the candidate path...");

            /* Read in the candidate path. */
            string pathNodesFileName =
                System.IO.Path.Combine(projectConfig.locationTempDir, config.TEMP_PATH_NODES);
            TextReader pathNodesReader = new StreamReader(pathNodesFileName);
            string[] pathNodesArray = pathNodesReader.ReadLine().Split(' ');
            pathNodesReader.Close();

            List<uint> pathNodes = new List<uint>();
            foreach (string pathNode in pathNodesArray)
            {
                if (pathNode != "")
                {
                    pathNodes.Add(Convert.ToUInt32(pathNode));
                }
            }

            /* Convert the adjusted block IDs in the candidate path to the actual block IDs. */
            Phx.Graphs.FlowGraph flowGraph = functionUnit.FlowGraph;
            List<Phx.Graphs.BasicBlock> pathBlocks = new List<Phx.Graphs.BasicBlock>();
            int position = 0;
            while (position < pathNodes.Count)
            {
                uint adjustedBlockId = pathNodes[position];
                uint actualBlockId = adjustedToActual[adjustedBlockId];
                
                Phx.Graphs.BasicBlock actualBlock =
                    flowGraph.Node(actualBlockId) as Phx.Graphs.BasicBlock;
                pathBlocks.Add(actualBlock);

                /* Skip over the "fake" intermediate nodes. */
                position = position + 2;
            }

            Path path = new Path(pathBlocks, config, projectConfig);
            Console.Out.WriteLine("PHOENIX: Finished reading and processing the candidate path.");

            /* Generate the conditions and the assignments along the path. */
            Console.Out.WriteLine("PHOENIX: Generating the conditions and " +
                "assignments along the path...");
            Console.Out.WriteLine();
            path.GenerateConditionsAndAssignments();
            Console.Out.WriteLine("PHOENIX: Finished generating the conditions and assignments.");

            if (path.ProjectConfig.debugConfig.DUMP_PATH)
            {
                path.Dump();
                Console.Out.WriteLine();
            }

            List<Pair<Expression, uint>> conditions = path.Conditions;
            HashSet<Expression> arrayVariables = path.ArrayVariables;
            Dictionary<Expression, List<uint>> arrayDimensions = path.ArrayDimensions;

            Console.Out.WriteLine("PHOENIX: Writing information about the path " +
                "to temporary files...");

            Console.Out.WriteLine("PHOENIX: Writing the conditions and assignments...");

            string pathConditionsFileName =
                System.IO.Path.Combine(projectConfig.locationTempDir, config.TEMP_PATH_CONDITIONS);
            StreamWriter pathConditionsWriter = new StreamWriter(pathConditionsFileName, false);
            pathConditionsWriter.AutoFlush = true;

            List<Expression> conditionExprs = new List<Expression>();
            foreach (Pair<Expression, uint> conditionPair in conditions)
            {
                Expression condition = conditionPair.First;
                pathConditionsWriter.WriteLine(condition);
                conditionExprs.Add(condition);
            }

            pathConditionsWriter.Close();

            Console.Out.WriteLine("PHOENIX: Writing completed.");

            Console.Out.WriteLine("PHOENIX: Writing line numbers...");
            path.DumpLineNumbers();
            Console.Out.WriteLine("PHOENIX: Writing completed.");

            Console.Out.WriteLine("PHOENIX: Writing the edges that correspond to " +
                "conditions and assignments...");
            /* The actual source node that corresponds to a constraint (condition
             * or assignment) is the dummy node after the adjusted source node. */
            path.DumpConditionEdges(sourceNodeId => nodeSuccessors[actualToAdjusted[sourceNodeId]],
                sinkNodeId => actualToAdjusted[sinkNodeId]);
            Console.Out.WriteLine("PHOENIX: Writing completed.");

            Console.Out.WriteLine("PHOENIX: Writing the line numbers and truth values of " +
                "the conditional points..");
            path.DumpConditionTruths();
            Console.Out.WriteLine("PHOENIX: Writing completed.");

            Console.Out.WriteLine("PHOENIX: Writing information about array and " +
                "aggregate accesses...");
            path.DumpAccesses();
            Console.Out.WriteLine("PHOENIX: Writing completed.");

            if (path.ProjectConfig.debugConfig.DUMP_ALL_PATHS)
            {
                string allPathsFileName =
                    System.IO.Path.Combine(projectConfig.locationTempDir, config.TEMP_PATH_ALL);
                StreamWriter allPathsWriter = new StreamWriter(allPathsFileName, true);
                allPathsWriter.AutoFlush = true;
                allPathsWriter.Write("*** CANDIDATE PATH ***");
                allPathsWriter.WriteLine(path.ToString());
                allPathsWriter.Close();
            }

            Console.Out.WriteLine("PHOENIX: Path information written to temporary files.");

            Console.Out.WriteLine("PHOENIX: Writing the corresponding SMT query to " +
                "a temporary file...");

            string smtQueryFileName =
                System.IO.Path.Combine(projectConfig.locationTempDir,
                    config.TEMP_PATH_QUERY + ".smt");
            StreamWriter smtQueryWriter = new StreamWriter(smtQueryFileName, false);
            smtQueryWriter.AutoFlush = true;
            smtQueryWriter.Write(SmtHelper.ConvertToSmtLib2Query(path));
            smtQueryWriter.Close();

            Console.Out.WriteLine("PHOENIX: Writing completed.");
        }

        /// <summary>
        /// Splits the BasicBlock in <paramref name="functionUnit"/> that contains a user-defined
        /// label with the name <paramref name="name"/>, and returns the new BasicBlock as a
        /// result of the split.
        /// </summary>
        /// 
        /// <param name="functionUnit">Function unit that contains the label.</param>
        /// <param name="name">Name of the user-defined label to find.</param>
        /// <returns>New BasicBlock in <paramref name="functionUnit"/>
        /// that results from a split at the user-defined label with the name
        /// <paramref name="name"/>; null otherwise.</returns>
        /// <remarks>Precondition: There is only one user-defined label with
        /// the name <paramref name="name"/>.</remarks>
        public static Phx.Graphs.BasicBlock SplitAtUserLabel(Phx.FunctionUnit functionUnit,
            string name)
        {
            Phx.Graphs.FlowGraph flowGraph = functionUnit.FlowGraph;
            Phx.Graphs.BasicBlock result = null;

            foreach (Phx.IR.Instruction instruction in functionUnit.Instructions)
            {
                if (instruction.Opcode == Phx.Common.Opcode.UserLabel)
                {
                    Phx.IR.Operand userLabelOperand = instruction.SourceOperand;
                    string operandName = userLabelOperand.ToString();

                    /* Fix the label name, if needed. */
                    if (operandName.StartsWith("$"))
                    {
                        operandName = operandName.Substring(1);
                    }

                    if (operandName == name)
                    {
                        Phx.Graphs.BasicBlock block = instruction.BasicBlock;
                        result = flowGraph.SplitBlock(block, instruction);
                        break;
                    }
                }
            }

            if (result == null)
            {
                Console.Out.WriteLine("PHOENIX: Cannot find user-defined label: " + name);
                Console.Out.WriteLine("Exiting GameTime.");
                Environment.Exit(1);
            }

            return result;
        }
    }
}
