﻿namespace Gu.Roslyn.Asserts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    /// <summary>
    /// Specify a default metadata reference to use.
    /// </summary>
    [Obsolete("Use Settings.Default")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class MetadataReferenceAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataReferenceAttribute"/> class.
        /// </summary>
        /// <param name="type">A type in the assembly.</param>
        public MetadataReferenceAttribute(Type type)
            : this(type, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetadataReferenceAttribute"/> class.
        /// </summary>
        /// <param name="type">A type in the assembly.</param>
        /// <param name="aliases">Aliases: ex {"global", "mscorlib"} can be null.</param>
        public MetadataReferenceAttribute(Type type, string[]? aliases)
        {
            this.Type = type ?? throw new ArgumentNullException(nameof(type));
            this.Aliases = aliases ?? new string[0];
            if (this.Aliases.Count == 0)
            {
                this.MetadataReference = MetadataReferences.CreateFromAssembly(type.Assembly);
            }

            this.MetadataReference = MetadataReferences.CreateFromAssembly(type.Assembly).WithAliases(this.Aliases);
            Settings.Default = Settings.Default.WithMetadataReferences(x => x.Append(this.MetadataReference));
        }

        /// <summary>
        /// Gets the type in the assembly.
        /// </summary>
        public Type Type { get; }

        /// <summary>
        /// Gets the aliases.
        /// </summary>
        public IReadOnlyList<string> Aliases { get; }

        /// <summary>
        /// Gets the reference, this only works when net46.
        /// </summary>
        public MetadataReference MetadataReference { get; }
    }
}
