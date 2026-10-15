using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using SocialEvolution.Domain;
using SocialEvolution.World;
using Xunit;

namespace SocialEvolution.Tests
{
    public class ArchitectureGuardTests
    {
        [Fact]
        public void CatalogsAreTheP0Sets()
        {
            Assert.Equal(new[] { "Food", "Wood", "Tool" }, Enum.GetNames(typeof(Good)));
            Assert.Equal(
                new[] { "Farm", "Factory", "Housing", "Market", "Government" },
                Enum.GetNames(typeof(BuildingKind)));
        }

        [Fact]
        public void PureAssembliesDoNotReferenceUnity()
        {
            foreach (Assembly assembly in RuntimeAssemblies())
            {
                foreach (AssemblyName reference in assembly.GetReferencedAssemblies())
                {
                    Assert.DoesNotContain("Unity", reference.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        [Fact]
        public void AsmdefsLockEngineReferences()
        {
            Assert.True(NoEngine("Core"));
            Assert.True(NoEngine("Domain"));
            Assert.True(NoEngine("World"));
            Assert.False(NoEngine("Bootstrap"));

            Assert.Empty(References("Domain"));
            Assert.Empty(References("Core"));
            Assert.Equal(new[] { "SocialEvolution.Domain" }, References("World"));
            Assert.Equal(
                new[] { "SocialEvolution.Core", "SocialEvolution.Domain", "SocialEvolution.World" },
                References("Bootstrap"));
        }

        [Fact]
        public void SourcesStayOnTheRightSideOfTheEngine()
        {
            string root = RepoRoot();
            AssertNoEngine(Path.Combine(root, "Assets", "Scripts", "Core"));
            AssertNoEngine(Path.Combine(root, "Assets", "Scripts", "Domain"));
            AssertNoEngine(Path.Combine(root, "Assets", "Scripts", "World"));
            AssertNoEngineFile(Path.Combine(root, "Assets", "Scripts", "Bootstrap", "OpeningSeed.cs"));
            AssertNoEngineFile(Path.Combine(root, "Assets", "Scripts", "Bootstrap", "GameSession.cs"));

            string driver = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "Bootstrap", "Unity", "SimulationDriver.cs"));
            Assert.Contains("MonoBehaviour", driver, StringComparison.Ordinal);
            Assert.Contains("AdvanceRealtime", driver, StringComparison.Ordinal);
        }

        [Fact]
        public void NoDirectAssignmentOrPrivateEnterpriseApi()
        {
            foreach (Assembly assembly in RuntimeAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.Name.Contains('<'))
                    {
                        continue;
                    }

                    Assert.DoesNotContain("Company", type.Name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("Enterprise", type.Name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("PrivateFirm", type.Name, StringComparison.OrdinalIgnoreCase);
                    Assert.DoesNotContain("Recruitment", type.Name, StringComparison.OrdinalIgnoreCase);

                    const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                    foreach (MethodInfo method in type.GetMethods(flags))
                    {
                        AssertNoAssignment(type, method.Name, method.GetParameters());
                    }

                    foreach (ConstructorInfo constructor in type.GetConstructors(flags))
                    {
                        AssertNoAssignment(type, constructor.Name, constructor.GetParameters());
                    }
                }
            }

            AssertNoEmploymentStorage(typeof(JobPosting));
            AssertNoEmploymentStorage(typeof(Building));
            Assert.Null(typeof(JobPosting).GetProperty("VacantSlots"));
            Assert.Null(typeof(JobPosting).GetProperty("FilledCount"));

            MethodInfo? setPosting = typeof(Building).GetMethod(
                "SetJobPosting",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(setPosting);
            Assert.True(setPosting!.IsAssembly);
            Assert.False(setPosting.IsPublic);
        }

        private static void AssertNoAssignment(Type type, string name, ParameterInfo[] parameters)
        {
            bool assignsCitizen = name.Contains("AssignCitizen", StringComparison.OrdinalIgnoreCase)
                || (name.Contains("Assign", StringComparison.OrdinalIgnoreCase)
                    && (name.Contains("Citizen", StringComparison.OrdinalIgnoreCase) || name.Contains("Job", StringComparison.OrdinalIgnoreCase)))
                || name.Contains("HireCitizen", StringComparison.OrdinalIgnoreCase)
                || name.Contains("ForceEmploy", StringComparison.OrdinalIgnoreCase);
            if (assignsCitizen)
            {
                throw new Xunit.Sdk.XunitException($"{type.FullName}.{name} looks like a direct assignment API.");
            }

            // 住所可以同时出现市民和建筑编号。派工才会同时出现市民和岗位。
            bool citizen = parameters.Any(parameter => IsCitizen(parameter.ParameterType));
            bool job = parameters.Any(parameter => IsJob(parameter.ParameterType));
            if (citizen && job)
            {
                throw new Xunit.Sdk.XunitException($"{type.FullName}.{name} takes both a citizen and a job.");
            }
        }

        private static void AssertNoEmploymentStorage(Type type)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (FieldInfo field in type.GetFields(flags))
            {
                Assert.False(ContainsCitizen(field.FieldType));
            }

            foreach (PropertyInfo property in type.GetProperties(flags))
            {
                Assert.False(ContainsCitizen(property.PropertyType));
                Assert.NotEqual("FilledCount", property.Name);
            }
        }

        private static bool IsCitizen(Type type) => type == typeof(Citizen) || type == typeof(CitizenId);

        private static bool IsJob(Type type) => type == typeof(JobPosting) || type == typeof(JobPostingId);

        private static bool ContainsCitizen(Type? type)
        {
            if (type == null)
            {
                return false;
            }

            if (IsCitizen(type))
            {
                return true;
            }

            if (type.IsArray)
            {
                return ContainsCitizen(type.GetElementType());
            }

            if (!type.IsGenericType)
            {
                return false;
            }

            Type[] arguments = type.GetGenericArguments();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (ContainsCitizen(arguments[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static Assembly[] RuntimeAssemblies()
        {
            return new[]
            {
                typeof(SocialEvolution.Core.TimeService).Assembly,
                typeof(Citizen).Assembly,
                typeof(WorldState).Assembly,
                typeof(SocialEvolution.Bootstrap.GameSession).Assembly,
            };
        }

        private static void AssertNoEngine(string directory)
        {
            foreach (string file in Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                AssertNoEngineFile(file);
            }
        }

        private static void AssertNoEngineFile(string file)
        {
            string text = File.ReadAllText(file);
            Assert.DoesNotContain("UnityEngine", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MonoBehaviour", text, StringComparison.Ordinal);
            Assert.DoesNotContain("AssignCitizenToJob", text, StringComparison.Ordinal);
        }

        private static bool NoEngine(string layer)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(AsmdefPath(layer)));
            return document.RootElement.GetProperty("noEngineReferences").GetBoolean();
        }

        private static string[] References(string layer)
        {
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(AsmdefPath(layer)));
            return document.RootElement.GetProperty("references").EnumerateArray().Select(item => item.GetString() ?? string.Empty).ToArray();
        }

        private static string AsmdefPath(string layer)
        {
            return Path.Combine(RepoRoot(), "Assets", "Scripts", layer, $"SocialEvolution.{layer}.asmdef");
        }

        private static string RepoRoot()
        {
            DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Assets", "Scripts", "Domain")))
                {
                    return dir.FullName;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException("找不到仓库根目录。");
        }
    }
}
