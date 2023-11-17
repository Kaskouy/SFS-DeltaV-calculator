using SFS.Parts.Modules;
using SFS.Parts;
using SFS.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static SFS.Parts.Modules.FlowModule;
using SFS;
using System.Net.Sockets;
using UnityEngine;

namespace DeltaV_Calculator
{
    public class DeltaV_Simulator
    {

        // Class SimulatedFuelTank
        // ---------------------
        // A minimal and local representation of a fuel tank to allow us to perform a simulation
        // A "fuel tank" is to be seen here as a cluster of fuel tanks all connected together that form a single entity
        private class SimulatedFuelTank
        {
            // Initial data
            public double fuelMass;
            public List<SimulatedEngine> associatedEngines = new List<SimulatedEngine>();

            // Data that will vary with the simulation (fuelMass will evolve too)
            public double consumptionRate;
            public double burnTime;
            
            // Constructor
            public SimulatedFuelTank(ResourceModule resourceModule)
            {
                this.fuelMass = resourceModule.ResourceAmount;
            }

            public SimulatedFuelTank(double resourceAmount)
            {
                this.fuelMass = resourceAmount;
            }

            //Calculate the internal variables: consumptionRate and burnTime
            public void CalculateData()
            {
                consumptionRate = 0.0;

                foreach (SimulatedEngine simulatedEngine in associatedEngines)
                {
                    double consumptionRatio = 1.0;

                    if ((0 < fuelMass) && (fuelMass < simulatedEngine.totalAssociatedFuelMass))
                    {
                        // because the engine drains fuel from all the tanks it touches in equal proportions; the condition is to ensure that we don't have a "0/0" or an absurd value.
                        consumptionRatio = fuelMass / simulatedEngine.totalAssociatedFuelMass;
                    }

                    consumptionRate += simulatedEngine.consumption * consumptionRatio;
                }

                burnTime = fuelMass / consumptionRate;
            }

            public void ApplyBurnDuration(double appliedBurnTime)
            {
                fuelMass -= consumptionRate * appliedBurnTime;
                if (fuelMass < 0.0) fuelMass = 0.0; // In case of numerical imprecision error

                burnTime -= appliedBurnTime;
                if(burnTime < 0.0) burnTime = 0.0; // In case of numerical imprecision error
            }

            public bool isEmpty()
            {
                // Below this threshold (1 millisecond of remaining fuel), the tank content is considered empty, because
                // we wouldn't go far with that... a small value instead of 0 allows to fix some numerical imprecision errors.
                return (burnTime < 0.001); 
            }
        }


        // Class SimulatedEngine
        // ---------------------
        // A minimal and local representation of an engine to allow us to perform a simulation
        private class SimulatedEngine
        {
            // Initial data
            public double thrust;
            public double Isp;
            public double consumption;
            public Vector2 direction;

            // variables for the simulation
            public double totalAssociatedFuelMass;
            public List<SimulatedFuelTank> associatedFuelTanks = new List<SimulatedFuelTank>();

            // Constructor
            public SimulatedEngine(EngineModule engineModule) 
            {
                direction = (Vector2)engineModule.transform.TransformVector(engineModule.thrustNormal.Value);

                // For cheaters...
                float stretchFactor = 1.0f;
                if(Base.worldBase.AllowsCheats) stretchFactor = direction.magnitude;

                this.thrust = stretchFactor * engineModule.thrust.Value;
                this.Isp = engineModule.ISP.Value * Base.worldBase.settings.difficulty.IspMultiplier;
                consumption = thrust / Isp;
                direction = direction.normalized;
            }

            public void CalculateData()
            {
                totalAssociatedFuelMass = 0.0;

                foreach (SimulatedFuelTank simulatedFuelTank in associatedFuelTanks)
                {
                    totalAssociatedFuelMass += simulatedFuelTank.fuelMass;
                }
            }
        }


        // Method CalculateGlobalIspAndConsumption
        // ---------------------------------------
        // Calculates the specific impulse and fuel conscumption for a whole set of different engines
        private static void CalculateGlobalIspAndConsumption(List<SimulatedEngine> listSimulatedEngines, out double globalIsp, out double globalConsumption)
        {
            double globalThrust = 0.0;
            globalConsumption = 0.0;

            foreach(SimulatedEngine simulatedEngine in listSimulatedEngines)
            {
                globalThrust += simulatedEngine.thrust;
                globalConsumption += simulatedEngine.consumption;
            }

            globalIsp = globalThrust / globalConsumption;
        }


        // Method CalculateGlobalDeltaVvector
        // ----------------------------------
        // Calculates the direction in which will be applied the deltaV. The magnitude indicates how much of the thrust
        // is converted into delta-V - If the engines don't push in the same direction there will be losses.
        private static Vector2 CalculateGlobalDeltaVvector(List<SimulatedEngine> listSimulatedEngines)
        {
            Vector2 dvVector = new Vector2(0.0f, 0.0f);
            float totalThrust = 0.0f;

            foreach(SimulatedEngine simulatedEngine in listSimulatedEngines)
            {
                dvVector += simulatedEngine.direction * (float)simulatedEngine.thrust; // Delta-V provided by an engine is considered proportional to its thrust
                totalThrust += (float)simulatedEngine.thrust;
            }

            dvVector = dvVector / totalThrust;

            return dvVector;
        }


        // Method CleanSimulatedDataLists
        // ------------------------------
        // This method will clean the lists of simulated fuel tanks and simulated engines: every empty fuel tank
        // will be discarded, every engine that is not connected to a non-empty fuel tank anymore will be removed.
        private static void CleanSimulatedDataLists(List<SimulatedFuelTank> listSimulatedFuelTanks, List<SimulatedEngine> listSimulatedEngines)
        {
            // Note: the lists are browsed backwards so that we can remove elements while iterating on them

            // First, loop through the simulated tanks and remove those who are empty from the global list, and from the list associated to each engine
            for(int i_tank = listSimulatedFuelTanks.Count - 1; i_tank >= 0; i_tank--)
            {
                SimulatedFuelTank simulatedFuelTank = listSimulatedFuelTanks[i_tank];

                if(simulatedFuelTank.isEmpty()) // If tank is empty...
                {
                    // Remove the fuel tanks from the lists associated to engines
                    for(int i_engine = listSimulatedEngines.Count - 1; i_engine >= 0; i_engine--)
                    {
                        listSimulatedEngines[i_engine].associatedFuelTanks.Remove(simulatedFuelTank);
                    }

                    // Remove the fuel tank from the tank list
                    listSimulatedFuelTanks.RemoveAt(i_tank);
                }
            }

            // Secondly, some engines may be associated to no fuel tank now --> remove them from the global list of engines, and from the engine list associated to fuel tanks
            for (int i_engine = listSimulatedEngines.Count - 1; i_engine >= 0; i_engine--)
            {
                SimulatedEngine simulatedEngine = listSimulatedEngines[i_engine];

                if(!simulatedEngine.associatedFuelTanks.Any())
                {
                    // Engine is no longer associated to fuel tanks; remove it from the lists associated to fuel tanks where it could still appear...
                    for (int i_tank = listSimulatedFuelTanks.Count - 1; i_tank >= 0; i_tank--)
                    {
                        listSimulatedFuelTanks[i_tank].associatedEngines.Remove(simulatedEngine);
                    }

                    //... then from the main list
                    listSimulatedEngines.RemoveAt(i_engine);
                }
            }
        }


        // ---------------
        //
        // GENERAL METHODS
        //
        // ---------------

        // Method BuildResourceEngineAssociation
        // -------------------------------------
        // That method will browse the rocket parts, and will build some association tables between the ResourceModules (fuel source) and
        // the engines that exploit them. This will be the raw data for the algorithm.
        private static void BuildResourceEngineAssociation(Rocket rocket, out Dictionary<ResourceModule, List<EngineModule>> dictionaryResourceEngine_Surfaces, out Dictionary<ResourceModule, List<EngineModule>> dictionaryResourceEngine_Global)
        {
            EngineModule[] modules = rocket.partHolder.GetModules<EngineModule>();

            dictionaryResourceEngine_Surfaces = new Dictionary<ResourceModule, List<EngineModule>>();
            dictionaryResourceEngine_Global = new Dictionary<ResourceModule, List<EngineModule>>();

            foreach (EngineModule engineModule in modules)
            {
                if (engineModule.engineOn.Value) // Engine has to be on
                {
                    foreach (Flow flow in engineModule.source.sources)
                    {
                        if (flow.sourceSearchMode == SourceMode.Surfaces) // ignore ion engine for now (flow.sourceSearchMode == SourceMode.Global)
                        {
                            foreach (ResourceModule resourceModule in flow.sources)
                            {
                                bool exists = dictionaryResourceEngine_Surfaces.TryGetValue(resourceModule, out List<EngineModule> engineModuleList);

                                if (exists)
                                {
                                    // Add this engine to the engine module list associated to this resource module
                                    engineModuleList.Add(engineModule);
                                }
                                else
                                {
                                    // Add a new resource module into the dictionary, with the engineModule associated
                                    dictionaryResourceEngine_Surfaces.Add(resourceModule, new List<EngineModule> { engineModule });
                                }
                            }
                        }
                        else if(flow.sourceSearchMode == SourceMode.Global)
                        {
                            foreach (ResourceModule resourceModule in flow.sources)
                            {
                                bool exists = dictionaryResourceEngine_Global.TryGetValue(resourceModule, out List<EngineModule> engineModuleList);

                                if (exists)
                                {
                                    // Add this engine to the engine module list associated to this resource module
                                    engineModuleList.Add(engineModule);
                                }
                                else
                                {
                                    // Add a new resource module into the dictionary, with the engineModule associated
                                    dictionaryResourceEngine_Global.Add(resourceModule, new List<EngineModule> { engineModule });
                                }
                            }
                        }
                    }
                }
            }
        }


        // Method BuildSimulatedData
        // -------------------------
        // That method will build the necessary data for the simulation. It needs in input the list of all resourceModules
        // associated with their engines
        private static void BuildSimulatedData(Dictionary<ResourceModule, List<EngineModule>> dictionaryResourceEngine_Surfaces, Dictionary<ResourceModule, List<EngineModule>> dictionaryResourceEngine_Global, out List<SimulatedFuelTank> listSimulatedFuelTanks, out List<SimulatedEngine> listSimulatedEngines)
        {
            listSimulatedFuelTanks = new List<SimulatedFuelTank>();
            listSimulatedEngines = new List<SimulatedEngine>();

            // First build a dictionary that associates the engine modules to each simulated engine
            // ------------------------------------------------------------------------------------
            Dictionary<EngineModule, SimulatedEngine> dictionaryEngines = new Dictionary<EngineModule, SimulatedEngine>();

            // For the "Surface" ResourceModules (those who are associated with an engine only if they are physically linked to it)
            foreach (KeyValuePair<ResourceModule, List<EngineModule>> keyValuePair in dictionaryResourceEngine_Surfaces)
            {
                foreach (EngineModule engineModule in keyValuePair.Value)
                {
                    if (!dictionaryEngines.ContainsKey(engineModule))
                    {
                        dictionaryEngines.Add(engineModule, new SimulatedEngine(engineModule));
                    }
                }
            }

            // For the "Global" ResourceModules (those associated to engines that pump the fuel from the whole craft - ion engines)
            foreach (KeyValuePair<ResourceModule, List<EngineModule>> keyValuePair in dictionaryResourceEngine_Global)
            {
                foreach (EngineModule engineModule in keyValuePair.Value)
                {
                    if (!dictionaryEngines.ContainsKey(engineModule))
                    {
                        dictionaryEngines.Add(engineModule, new SimulatedEngine(engineModule));
                    }
                }
            }


            // Then build the list of simulated fuel tanks with their associated simulated engines
            // -----------------------------------------------------------------------------------
            foreach (KeyValuePair<ResourceModule, List<EngineModule>> keyValuePair in dictionaryResourceEngine_Surfaces)
            {
                SimulatedFuelTank simulatedFuelTank = new SimulatedFuelTank(keyValuePair.Key);

                // Add all engines associated to it through the ResourceModule
                foreach (EngineModule engineModule in keyValuePair.Value)
                {
                    SimulatedEngine simulatedEngine = dictionaryEngines[engineModule];
                    simulatedFuelTank.associatedEngines.Add(simulatedEngine); // Normally each engine is present only once, no need to test if it is already contained
                }

                // Also add the global engines that use the same resource
                foreach(KeyValuePair<ResourceModule, List<EngineModule>> keyValuePair_global in dictionaryResourceEngine_Global)
                {
                    if(keyValuePair_global.Key.resourceType == keyValuePair.Key.resourceType)
                    {
                        foreach (EngineModule engineModule in keyValuePair_global.Value)
                        {
                            SimulatedEngine simulatedEngine = dictionaryEngines[engineModule];
                            simulatedFuelTank.associatedEngines.Add(simulatedEngine);
                        }
                    }
                }

                listSimulatedFuelTanks.Add(simulatedFuelTank);
            }

            // Also build a simulated fuel tank for each global ResourceModule: the fuel mass from the local ResourceModules that are included
            // will be substracted so that it only includes the fuel that's not reached by anybody else. That way we can treat it as other modules.
            foreach (KeyValuePair<ResourceModule, List<EngineModule>> keyValuePair_global in dictionaryResourceEngine_Global)
            {
                double fuelMass = keyValuePair_global.Key.ResourceAmount;

                foreach (KeyValuePair<ResourceModule, List<EngineModule>> keyValuePair in dictionaryResourceEngine_Surfaces)
                {
                    if(keyValuePair_global.Key.resourceType == keyValuePair.Key.resourceType)
                    {
                        fuelMass -= keyValuePair.Key.ResourceAmount;
                    }
                }

                if(fuelMass > 0.0)
                {
                    // if mass positive (otherwise all fuel tanks can already be reached through the "local" ResourceModules), create a simulated fuel tank
                    SimulatedFuelTank simulatedFuelTank = new SimulatedFuelTank(fuelMass);

                    // add all the global engines obviously...
                    foreach(EngineModule engineModule in keyValuePair_global.Value)
                    {
                        simulatedFuelTank.associatedEngines.Add(dictionaryEngines[engineModule]);
                    }

                    // and add it to the list
                    listSimulatedFuelTanks.Add(simulatedFuelTank);
                }
            }


            // Finally, build the list of simulated engines
            // --------------------------------------------
            foreach (KeyValuePair<EngineModule, SimulatedEngine> keyValuePair in dictionaryEngines)
            {
                SimulatedEngine simulatedEngine = keyValuePair.Value;
                simulatedEngine.totalAssociatedFuelMass = 0.0;

                listSimulatedEngines.Add(simulatedEngine);

                // Build the association of fuel tanks with engines
                foreach (SimulatedFuelTank simulatedFuelTank in listSimulatedFuelTanks)
                {
                    if (simulatedFuelTank.associatedEngines.Contains(simulatedEngine))
                    {
                        simulatedEngine.associatedFuelTanks.Add(simulatedFuelTank);
                        simulatedEngine.totalAssociatedFuelMass += simulatedFuelTank.fuelMass;
                    }
                }
            }
        }

        // Method RunSimulation
        // --------------------
        // That method runs a simulation from the initialized simulated data:
        // It will try to make burn all engines until they run out of fuel and calculate the total delta-V that will be obtained from it
        private static double RunSimulation(double rocketMass, List<SimulatedFuelTank> listSimulatedFuelTanks, List<SimulatedEngine> listSimulatedEngines)
        {
            Vector2 totalDeltaV = new Vector2(0.0f, 0.0f);

            int watchdog = listSimulatedEngines.Count + 10; // We are supposed to iterate at most listSimulatedEngines.Count times

            while (listSimulatedEngines.Any() && (watchdog > 0))
            {
                double burnTime = double.PositiveInfinity;

                // Calculate the simulated variables
                // ---------------------------------
                foreach (SimulatedEngine simulatedEngine in listSimulatedEngines) // engines - it's important to do that one before tanks
                {
                    simulatedEngine.CalculateData();
                }

                for (int i = 0; i < listSimulatedFuelTanks.Count; i++) // tanks
                {
                    SimulatedFuelTank simulatedFuelTank = listSimulatedFuelTanks[i];
                    simulatedFuelTank.CalculateData();

                    // To memorize the first one that will run out of fuel
                    if (simulatedFuelTank.burnTime < burnTime)
                    {
                        burnTime = simulatedFuelTank.burnTime;
                    }
                }

                // Simulate the burn until the most critical tank is empty
                // -------------------------------------------------------
                CalculateGlobalIspAndConsumption(listSimulatedEngines, out double globalIsp, out double globalConsumption);

                Vector2 deltaV_Vector = CalculateGlobalDeltaVvector(listSimulatedEngines);

                double newRocketMass = rocketMass - globalConsumption * burnTime;

                // Your turn Mister Tsiolkovsky!
                totalDeltaV += (float)(9.8 * globalIsp * Math.Log(rocketMass / newRocketMass)) * deltaV_Vector;

                // Update remaining masses
                rocketMass = newRocketMass;

                foreach (SimulatedFuelTank simulatedFuelTank in listSimulatedFuelTanks)
                {
                    simulatedFuelTank.ApplyBurnDuration(burnTime);
                }

                // Remove empty fuel tanks and unused engines from the list, so that we only work with useful data
                // --------------------------------------------------------
                CleanSimulatedDataLists(listSimulatedFuelTanks, listSimulatedEngines);

                watchdog--;
            }

            return totalDeltaV.magnitude;
        }


        public static double CalculateDV(Rocket rocket)
        {
            // First, build a list of all tank clusters (ResourceModule) associated to at least one turned on engine
            // ------------------------------------------------------------------------------------------------------
            BuildResourceEngineAssociation(rocket, out Dictionary<ResourceModule, List<EngineModule>> dictionaryResourceEngine, out Dictionary<ResourceModule, List<EngineModule>> dictionaryResourceEngine_global);

            //UnityEngine.Debug.Log("Resource list has " + theList.Count + " elements");

            // Then, Recreate a set of simulated data, so that we can modify the values and simulate the burn
            // ----------------------------------------------------------------------------------------------
            BuildSimulatedData(dictionaryResourceEngine, dictionaryResourceEngine_global, out List<SimulatedFuelTank> listSimulatedFuelTanks, out List<SimulatedEngine> listSimulatedEngines);

            // Simulate the whole burn
            // -----------------------
            double totalDeltaV = RunSimulation(rocket.mass.GetMass(), listSimulatedFuelTanks, listSimulatedEngines);

            return totalDeltaV;
        }
    }
}
