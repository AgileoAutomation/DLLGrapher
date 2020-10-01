﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DotNETAssemblyGrapherModel;
using System.IO;

namespace DotNETAssemblyGrapherApplication
{
    public class ComponentsBuilder
    {
        List<ComponentSpecification> specifications;
        public ComponentsBuilder(string specFilePath)
        {
            FileInfo file = new FileInfo(specFilePath);
            if (file.Extension == ".json")
            {
                JSONParser parser = new JSONParser();
                specifications = parser.BuildSpecifications(specFilePath);
            }
            else if (file.Extension == ".xlsx")
            {
                ExcelParser parser = new ExcelParser();
                specifications = parser.BuildSpecifications(specFilePath);
            }
        }

        public void Build(Model model)
        {
            foreach (ComponentSpecification spec in specifications)
            {
                SoftwareComponent component = BuildComponent(model, spec);
                if (component != null)
                    model.SoftwareComponents.Add(component);
            }
        }

        private SoftwareComponent BuildComponent(Model model, ComponentSpecification spec)
        {
            List<AssemblyPointer> componentPointers = new List<AssemblyPointer>();
            List<SoftwareComponent> subcomponents = new List<SoftwareComponent>();

            foreach (ComponentSpecification subSpec in spec.Subcomponents)
            {
                SoftwareComponent subcomponent = BuildComponent(model, subSpec);
                if (subcomponent != null)
                    subcomponents.Add(subcomponent);
            }

            foreach (string assemblyname in spec.Assemblies)
            {
                if (assemblyname.Contains("*"))
                {
                    Regex regex = new Regex(assemblyname);

                    foreach (AssemblyPointer pointer in model.AllAssemblies.Where(x => regex.Match(x.AssemblyName.Name).Success))
                    {
                        if (pointer.FindProperty("Component") == null)
                        {
                            componentPointers.Add(pointer);
                            pointer.AddProperty("Component", spec.Name);
                        }
                    }
                }
                else
                {
                    AssemblyPointer pointer = model.FindAssemblyById(assemblyname);
                    if (pointer != null && pointer.FindProperty("Component") == null)
                    {
                        componentPointers.Add(pointer);
                        pointer.AddProperty("Component", spec.Name);
                    }
                }
            }

            try
            {
                SoftwareComponent component = new SoftwareComponent(spec.Name, componentPointers, spec.Color, subcomponents);

                return component;
            }
            catch (ArgumentException e)
            {
                return null;
            }
        }
    }
}
