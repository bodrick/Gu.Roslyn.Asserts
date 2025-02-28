﻿namespace Gu.Roslyn.Asserts
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Reflection;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Helper for getting meta data references from <see cref="MetadataReferenceAttribute"/> and <see cref="MetadataReferencesAttribute"/>.
    /// </summary>
    public static partial class MetadataReferences
    {
        private static ImmutableArray<MetadataReference> fromAttributes;

        /// <summary>
        /// Create a <see cref="MetadataReference"/> for the <paramref name="assembly"/>.
        /// Checks reference assemblies first.
        /// </summary>
        /// <param name="assembly">An <see cref="Assembly"/>.</param>
        /// <returns>A <see cref="MetadataReference"/>.</returns>
        public static MetadataReference CreateFromAssembly(Assembly assembly)
        {
            if (ReferenceAssembly.TryGet(assembly, out var reference))
            {
                return reference;
            }

            return MetadataReference.CreateFromFile(assembly.Location);
        }

        /// <summary>
        /// Create a <see cref="MetadataReference"/> for the <paramref name="assemblyFile"/>.
        /// Checks reference assemblies first.
        /// </summary>
        /// <param name="assemblyFile">An <see cref="Assembly"/>.</param>
        /// <returns>A <see cref="MetadataReference"/>.</returns>
        public static MetadataReference CreateFromFile(string assemblyFile)
        {
            if (ReferenceAssembly.TryGet(assemblyFile, out var reference))
            {
                return reference;
            }

            return MetadataReference.CreateFromFile(assemblyFile);
        }

        /// <summary>
        /// Get the <see cref="MetadataReference"/> for <paramref name="typesInAssemblies"/> and all assemblies referenced by <paramref name="typesInAssemblies"/>.
        /// </summary>
        /// <param name="typesInAssemblies">A type in the assemblies.</param>
        /// <returns><see cref="MetadataReference"/>s.</returns>
        public static IEnumerable<MetadataReference> Transitive(params Type[] typesInAssemblies)
        {
            return Transitive(typesInAssemblies.SelectMany(t => Assemblies(t)).ToArray());

            static IEnumerable<Assembly> Assemblies(Type type)
            {
                yield return type.Assembly;
                if (type.IsGenericType)
                {
                    foreach (var genericArgument in type.GetGenericArguments())
                    {
                        yield return genericArgument.Assembly;
                    }
                }
            }
        }

        /// <summary>
        /// Get the <see cref="MetadataReference"/> for <paramref name="assemblies"/> and all assemblies referenced by <paramref name="assemblies"/>.
        /// </summary>
        /// <param name="assemblies">The assemblies.</param>
        /// <returns><see cref="MetadataReference"/>s.</returns>
        public static IEnumerable<MetadataReference> Transitive(params Assembly[] assemblies)
        {
            if (assemblies is null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            foreach (var assembly in RecursiveReferencedAssemblies(assemblies))
            {
                yield return CreateFromAssembly(assembly);
            }
        }

        private static HashSet<Assembly> RecursiveReferencedAssemblies(Assembly a, HashSet<Assembly>? recursiveAssemblies = null)
        {
            recursiveAssemblies ??= new HashSet<Assembly>();
            if (recursiveAssemblies.Add(a))
            {
                foreach (var referencedAssemblyName in a.GetReferencedAssemblies())
                {
                    if (TryGetOrLoad(referencedAssemblyName, out var referencedAssembly))
                    {
                        _ = RecursiveReferencedAssemblies(referencedAssembly, recursiveAssemblies);
                    }
                }
            }

            return recursiveAssemblies;

            bool TryGetOrLoad(AssemblyName name, out Assembly result)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
#pragma warning disable CS8601 // Possible null reference assignment.
                result = assemblies.SingleOrDefault(x => IsMatch(x.GetName()));
#pragma warning restore CS8601 // Possible null reference assignment.
                if (result != null)
                {
                    return true;
                }

                try
                {
                    result = Assembly.Load(name);
                    return true;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    return false;
                }

                bool IsMatch(AssemblyName candidate)
                {
                    return AssemblyName.ReferenceMatchesDefinition(candidate, name) &&
                           candidate.Version == name.Version;
                }
            }
        }

        private static HashSet<Assembly> RecursiveReferencedAssemblies(Assembly[] assemblies, HashSet<Assembly>? recursiveAssemblies = null)
        {
            recursiveAssemblies ??= new HashSet<Assembly>();
            foreach (var assembly in assemblies)
            {
                RecursiveReferencedAssemblies(assembly, recursiveAssemblies);
            }

            return recursiveAssemblies;
        }
    }
}
