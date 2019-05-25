using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BonVoyage
{
    class BonVoyageScenario : ScenarioModule
    {
        public static BonVoyageScenario Instance { get; private set; }

        private ConfigNode scenarioNode;


        /// <summary>
        /// Constructor
        /// </summary>
        public BonVoyageScenario()
        {
            Instance = this;
        }


        /// <summary>
        /// Load data
        /// </summary>
        /// <param name="gameNode"></param>
        public override void OnLoad(ConfigNode gameNode)
        {
            base.OnLoad(gameNode);
            scenarioNode = gameNode;
        }


        /// <summary>
        /// Save data
        /// </summary>
        /// <param name="gameNode"></param>
        public override void OnSave(ConfigNode gameNode)
        {
            base.OnSave(gameNode);
            SaveScenario(gameNode);
        }


        /// <summary>
        /// Save scenario details for each vessel with BV controller
        /// </summary>
        /// <param name="node"></param>
        private void SaveScenario(ConfigNode gameNode)
        {
            gameNode.ClearNodes();

            var controllers = BonVoyage.Instance.BVControllers;
            int count = controllers.Count;
            for (int i = 0; i < count; i++)
            {
                if (controllers[i].vessel != null)
                {
                    ConfigNode controllerNode = new ConfigNode("CONTROLLER");
                    controllerNode.AddValue("vesselId", controllers[i].vessel.id);

                    ConfigNode subNode = new ConfigNode("BATTERIES");
                    subNode.AddValue("useBatteries", controllers[i].batteries.UseBatteries);
                    subNode.AddValue("maxUsedEC", controllers[i].batteries.MaxUsedEC);
                    subNode.AddValue("ecPerSecondConsumed", controllers[i].batteries.ECPerSecondConsumed);
                    subNode.AddValue("ecPerSecondGenerated", controllers[i].batteries.ECPerSecondGenerated);
                    subNode.AddValue("currentEC", controllers[i].batteries.CurrentEC);
                    controllerNode.AddNode(subNode);

                    subNode = new ConfigNode("FUEL_CELLS");
                    subNode.AddValue("useFuelCells", controllers[i].fuelCells.Use);
                    subNode.AddValue("outputEC", controllers[i].fuelCells.OutputValue);
                    List<Resource> res = controllers[i].fuelCells.InputResources;
                    ConfigNode resourceNode;
                    for (int r = 0; r < res.Count; r++)
                    {
                        resourceNode = new ConfigNode("RESOURCE");
                        resourceNode.AddValue("name", res[r].Name);
                        resourceNode.AddValue("ratio", res[r].Ratio);
                        resourceNode.AddValue("maximumAmount", res[r].MaximumAmountAvailable);
                        resourceNode.AddValue("currentAmount", res[r].CurrentAmountUsed);
                        subNode.AddNode(resourceNode);
                    }
                    controllerNode.AddNode(subNode);

                    gameNode.AddNode(controllerNode);
                }
            }
        }


        /// <summary>
        /// Load scenario details for each vessel with BV controller
        /// </summary>
        public void LoadScenario()
        {
            if (scenarioNode != null)
            {
                var controllers = BonVoyage.Instance.BVControllers;
                int count = controllers.Count;
                for (int i = 0; i < count; i++)
                {
                    BVController controller = controllers[i];
                    if (controller.vessel != null)
                    {
                        ConfigNode controllerNode = scenarioNode.GetNode("CONTROLLER", "vesselId", controller.vessel.id.ToString());
                        if (controllerNode != null)
                        {
                            ConfigNode subNode = controllerNode.GetNode("BATTERIES");
                            if (subNode != null)
                            {
                                controller.batteries.UseBatteries = Convert.ToBoolean(subNode.GetValue("useBatteries"));
                                controller.batteries.MaxUsedEC = Convert.ToDouble(subNode.GetValue("maxUsedEC"));
                                controller.batteries.ECPerSecondConsumed = Convert.ToDouble(subNode.GetValue("ecPerSecondConsumed"));
                                controller.batteries.ECPerSecondGenerated = Convert.ToDouble(subNode.GetValue("ecPerSecondGenerated"));
                                controller.batteries.CurrentEC = Convert.ToDouble(subNode.GetValue("currentEC"));
                            }

                            subNode = controllerNode.GetNode("FUEL_CELLS");
                            if (subNode != null)
                            {
                                controller.fuelCells.Use = Convert.ToBoolean(subNode.GetValue("useFuelCells"));
                                controller.fuelCells.OutputValue = Convert.ToDouble(subNode.GetValue("outputEC"));
                                var resources = subNode.GetNodes("RESOURCE");
                                controller.fuelCells.InputResources.Clear();
                                for (int r = 0; r < resources.Length; r++)
                                {
                                    Resource ir = new Resource();
                                    ir.Name = resources[r].GetValue("name");
                                    ir.Ratio = Convert.ToDouble(resources[r].GetValue("ratio"));
                                    ir.MaximumAmountAvailable = Convert.ToDouble(resources[r].GetValue("maximumAmount"));
                                    ir.CurrentAmountUsed = Convert.ToDouble(resources[r].GetValue("currentAmount"));
                                    controller.fuelCells.InputResources.Add(ir);
                                }
                            }
                        }
                    }
                }
            }
        }

    }

}
