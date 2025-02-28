﻿namespace Gu.Roslyn.Asserts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using Gu.Roslyn.Asserts.Internals;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;

    /// <summary>
    /// Info about an expected diagnostic.
    /// </summary>
    [DebuggerDisplay("{Id} {Message} {Span}")]
    public partial class ExpectedDiagnostic
    {
        private static readonly FileLinePositionSpan NoPosition = new("MISSING", default, default);

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpectedDiagnostic"/> class.
        /// </summary>
        /// <param name="analyzer"> The analyzer that is expected to report a diagnostic.</param>
        /// <param name="span"> The position of the expected diagnostic.</param>
        [Obsolete("To be removed use factory methods.")]
        public ExpectedDiagnostic(DiagnosticAnalyzer analyzer, FileLinePositionSpan span)
        {
            this.Analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
            this.Message = null;
            this.Id = analyzer.SupportedDiagnostics[0].Id;
            this.Span = span;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpectedDiagnostic"/> class.
        /// </summary>
        /// <param name="id">The expected diagnostic ID, required.</param>
        /// <param name="message">The expected message, can be null. If null it is not checked in asserts.</param>
        /// <param name="span"> The position of the expected diagnostic.</param>
        public ExpectedDiagnostic(string id, string? message, FileLinePositionSpan span)
        {
            this.Id = id ?? throw new ArgumentNullException(nameof(id));
            this.Message = message;
            this.Span = span;
        }

        /// <summary>
        /// Gets the expected diagnostic ID.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the expected message as text.
        /// </summary>
        public string? Message { get; }

        /// <summary>
        /// Gets the position of the expected diagnostic.
        /// </summary>
        public FileLinePositionSpan Span { get; }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Span"/> has path specified.
        /// If the test is for a single file path can be omitted and will be set to 'MISSING'.
        /// </summary>
        public bool HasPath => this.Span.Path != null &&
                               this.Span.Path != NoPosition.Path;

        /// <summary>
        /// Gets a value indicating whether this instance indicates error position.
        /// </summary>
        public bool HasPosition => this.Span.StartLinePosition != NoPosition.StartLinePosition ||
                                   this.Span.EndLinePosition != NoPosition.EndLinePosition ||
                                   this.Span.Path != NoPosition.Path;

        /// <summary>
        /// Gets the analyzer that is expected to report a diagnostic.
        /// </summary>
        [Obsolete("To be removed.")]
        public DiagnosticAnalyzer? Analyzer { get; }

        /// <summary>
        /// Create a new instance of <see cref="ExpectedDiagnostic"/> and use the id from <paramref name="descriptor"/>.
        /// </summary>
        /// <param name="descriptor">The expected diagnostic descriptor.</param>
        /// <returns>A new instance of <see cref="ExpectedDiagnostic"/>.</returns>
        public static ExpectedDiagnostic Create(DiagnosticDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new ExpectedDiagnostic(descriptor.Id, null, NoPosition);
        }

        /// <summary>
        /// Create a new instance of <see cref="ExpectedDiagnostic"/> and use the suppressed id from <paramref name="descriptor"/>.
        /// </summary>
        /// <param name="descriptor">The expected suppression descriptor.</param>
        /// <returns>A new instance of <see cref="ExpectedDiagnostic"/>.</returns>
        public static ExpectedDiagnostic Create(SuppressionDescriptor descriptor)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            return new ExpectedDiagnostic(descriptor.SuppressedDiagnosticId, null, NoPosition);
        }

        /// <summary>
        /// Create a new instance of <see cref="ExpectedDiagnostic"/>.
        /// </summary>
        /// <param name="diagnosticId">The expected diagnostic id.</param>
        /// <returns>A new instance of <see cref="ExpectedDiagnostic"/>.</returns>
        public static ExpectedDiagnostic Create(string diagnosticId)
        {
            if (diagnosticId is null)
            {
                throw new ArgumentNullException(nameof(diagnosticId));
            }

            return new ExpectedDiagnostic(diagnosticId, null, NoPosition);
        }

        /// <summary>
        /// Create a new instance of <see cref="ExpectedDiagnostic"/>.
        /// </summary>
        /// <param name="diagnosticId">The expected diagnostic id.</param>
        /// <param name="message">The expected message.</param>
        /// <returns>A new instance of <see cref="ExpectedDiagnostic"/>.</returns>
        public static ExpectedDiagnostic Create(string diagnosticId, string message)
        {
            if (diagnosticId is null)
            {
                throw new ArgumentNullException(nameof(diagnosticId));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return new ExpectedDiagnostic(diagnosticId, message, NoPosition);
        }

        /// <summary>
        /// Create a new instance of <see cref="ExpectedDiagnostic"/>.
        /// </summary>
        /// <param name="diagnosticId">The expected diagnostic id.</param>
        /// <param name="line">The expected line number.</param>
        /// <param name="character">The expected character position.</param>
        /// <returns>A new instance of <see cref="ExpectedDiagnostic"/>.</returns>
        public static ExpectedDiagnostic Create(string diagnosticId, int line, int character)
        {
            if (diagnosticId is null)
            {
                throw new ArgumentNullException(nameof(diagnosticId));
            }

            return Create(diagnosticId, null, line, character);
        }

        /// <summary>
        /// Create a new instance of <see cref="ExpectedDiagnostic"/>.
        /// </summary>
        /// <param name="diagnosticId">The expected diagnostic id.</param>
        /// <param name="message">The expected message.</param>
        /// <param name="line">The expected line number.</param>
        /// <param name="character">The expected character position.</param>
        /// <returns>A new instance of <see cref="ExpectedDiagnostic"/>.</returns>
        public static ExpectedDiagnostic Create(string diagnosticId, string? message, int line, int character)
        {
            if (diagnosticId is null)
            {
                throw new ArgumentNullException(nameof(diagnosticId));
            }

            var position = new LinePosition(line, character);
            return new ExpectedDiagnostic(diagnosticId, message, new FileLinePositionSpan(NoPosition.Path, position, position));
        }

        /// <summary>
        /// Create a new instance of <see cref="ExpectedDiagnostic"/>.
        /// </summary>
        /// <param name="diagnosticId">The expected diagnostic id.</param>
        /// <param name="message">The expected message.</param>
        /// <param name="path">The path of the file with the diagnostic.</param>
        /// <param name="line">The expected line number.</param>
        /// <param name="character">The expected character position.</param>
        /// <returns>A new instance of <see cref="ExpectedDiagnostic"/>.</returns>
        public static ExpectedDiagnostic Create(string diagnosticId, string message, string path, int line, int character)
        {
            if (diagnosticId is null)
            {
                throw new ArgumentNullException(nameof(diagnosticId));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var position = new LinePosition(line, character);
            return new ExpectedDiagnostic(diagnosticId, message, new FileLinePositionSpan(path, position, position));
        }

        /// <summary>
        /// Create a new instance of <see cref="ExpectedDiagnostic"/>.
        /// </summary>
        /// <param name="diagnosticId">The expected diagnostic id.</param>
        /// <param name="markup">The code with diagnostic position indicated.</param>
        /// <param name="code"><paramref name="markup"/> without position indicated.</param>
        /// <returns>A new instance of <see cref="ExpectedDiagnostic"/>.</returns>
        public static ExpectedDiagnostic FromMarkup(string diagnosticId, string markup, out string code)
        {
            if (diagnosticId is null)
            {
                throw new ArgumentNullException(nameof(diagnosticId));
            }

            if (markup is null)
            {
                throw new ArgumentNullException(nameof(markup));
            }

            return FromMarkup(diagnosticId, null, markup, out code);
        }

        /// <summary>
        /// Create a new instance of <see cref="ExpectedDiagnostic"/>.
        /// </summary>
        /// <param name="diagnosticId">The expected diagnostic id.</param>
        /// <param name="message">The expected message.</param>
        /// <param name="markup">The code with diagnostic position indicated.</param>
        /// <param name="code"><paramref name="code"/> without position indicator.</param>
        /// <returns>A new instance of <see cref="ExpectedDiagnostic"/>.</returns>
        public static ExpectedDiagnostic FromMarkup(string diagnosticId, string? message, string markup, out string code)
        {
            if (diagnosticId is null)
            {
                throw new ArgumentNullException(nameof(diagnosticId));
            }

            if (markup is null)
            {
                throw new ArgumentNullException(nameof(markup));
            }

            var positions = CodeReader.FindLinePositions(markup).ToArray();
            if (positions.Length == 0)
            {
                throw new ArgumentException("Expected one error position indicated, was zero.", nameof(markup));
            }

            if (positions.Length > 1)
            {
                throw new ArgumentException($"Expected one error position indicated, was {positions.Length}.", nameof(markup));
            }

            code = markup.Replace("↓", string.Empty);
            var fileName = CodeReader.FileName(markup);
            var position = positions[0];
            return new ExpectedDiagnostic(diagnosticId, message, new FileLinePositionSpan(fileName, position, position));
        }

        /// <summary>
        /// Create a new instance of <see cref="ExpectedDiagnostic"/>.
        /// </summary>
        /// <param name="diagnosticId">The expected diagnostic id.</param>
        /// <param name="message">The expected message.</param>
        /// <param name="markup">The code with diagnostic position indicated.</param>
        /// <param name="code"><paramref name="markup"/> without position indicated.</param>
        /// <returns>A new instance of <see cref="ExpectedDiagnostic"/>.</returns>
        public static IReadOnlyList<ExpectedDiagnostic> ManyFromMarkup(string diagnosticId, string message, string markup, out string code)
        {
            if (diagnosticId is null)
            {
                throw new ArgumentNullException(nameof(diagnosticId));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (markup is null)
            {
                throw new ArgumentNullException(nameof(markup));
            }

            var positions = CodeReader.FindLinePositions(markup).ToArray();
            if (positions.Length == 0)
            {
                throw new ArgumentException("Expected one error position indicated, was zero.", nameof(markup));
            }

            code = markup.Replace("↓", string.Empty);
            var fileName = CodeReader.FileName(markup);
            return positions.Select(p => new ExpectedDiagnostic(diagnosticId, message, new FileLinePositionSpan(fileName, p, p)))
                            .ToArray();
        }

        /// <summary>
        /// Check if Id, Span and Message matches.
        /// If Message is nu it is not checked.
        /// </summary>
        public bool Matches(Diagnostic actual)
        {
            if (actual is null)
            {
                throw new ArgumentNullException(nameof(actual));
            }

            return this.Id == actual.Id &&
                   this.MessageMatches(actual) &&
                   (this.PositionMatches(actual) || actual.AdditionalLocations.Any(a => this.PositionMatches(a)));
        }

        /// <summary>
        /// Check if message matches if <see cref="Message"/> is not null.
        /// </summary>
        /// <param name="actual">The actual diagnostic.</param>
        /// <returns>True if match.</returns>
        public bool MessageMatches(Diagnostic actual)
        {
            if (actual is null)
            {
                throw new ArgumentNullException(nameof(actual));
            }

            return this.Message is null ||
                   this.Message == actual.GetMessage(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Check if position matches if specified.
        /// </summary>
        /// <param name="actual">The actual diagnostic.</param>
        /// <returns>True if match.</returns>
        public bool PositionMatches(Diagnostic actual)
        {
            if (actual is null)
            {
                throw new ArgumentNullException(nameof(actual));
            }

            return this.PositionMatches(actual.Location);
        }

        /// <summary>
        /// Get a clone of this instance with updated <see cref="Message"/>.
        /// </summary>
        /// <param name="message">The expected message.</param>
        /// <returns>A new <see cref="ExpectedDiagnostic"/>.</returns>
        public ExpectedDiagnostic WithMessage(string message) => new(this.Id, message, this.Span);

        /// <summary>
        /// Get a clone of this instance with updated <see cref="Span"/>.
        /// </summary>
        /// <param name="span">The expected position.</param>
        /// <returns>A new <see cref="ExpectedDiagnostic"/>.</returns>
        public ExpectedDiagnostic WithPosition(FileLinePositionSpan span) => new(this.Id, this.Message, span);

        /// <summary>
        /// Create a new instance of <see cref="ExpectedDiagnostic"/> with position.
        /// </summary>
        /// <param name="codeWithErrorsIndicated">The code with error position indicated..</param>
        /// <param name="cleanedSources"><paramref name="codeWithErrorsIndicated"/> without errors indicated.</param>
        /// <returns>A new instance of <see cref="ExpectedDiagnostic"/>.</returns>
        public ExpectedDiagnostic WithPositionFromCodeWithErrorsIndicated(string codeWithErrorsIndicated, out string cleanedSources)
        {
            if (codeWithErrorsIndicated is null)
            {
                throw new ArgumentNullException(nameof(codeWithErrorsIndicated));
            }

            var positions = CodeReader.FindLinePositions(codeWithErrorsIndicated).ToArray();
            if (positions.Length == 0)
            {
                throw new ArgumentException("Expected one error position indicated, was zero.", nameof(codeWithErrorsIndicated));
            }

            if (positions.Length > 1)
            {
                throw new ArgumentException($"Expected one error position indicated, was {positions.Length}.", nameof(codeWithErrorsIndicated));
            }

            cleanedSources = codeWithErrorsIndicated.Replace("↓", string.Empty);
            var fileName = CodeReader.FileName(codeWithErrorsIndicated);
            var position = positions[0];
            return new ExpectedDiagnostic(this.Id, this.Message, new FileLinePositionSpan(fileName, position, position));
        }

        /// <summary>
        /// Writes the diagnostic and the offending code.
        /// </summary>
        /// <returns>A string for use in assert exception.</returns>
        internal string ToString(IReadOnlyList<string> sources, string padding = "")
        {
            if (this.HasPosition)
            {
                var path = this.HasPath ? this.Span.Path : CodeReader.FileName(sources.Single());
                var match = sources.FirstOrDefault(x => CodeReader.FileName(x) == path && this.Span.Span.ExistsIn(x));
                var line = match != null ? CodeReader.GetLineWithErrorIndicated(match, this.Span.StartLinePosition) : string.Empty;
                return $"{padding}{this.Id} {this.Message}\r\n" +
                       $"{padding}  at line {this.Span.StartLinePosition.Line} and character {this.Span.StartLinePosition.Character} in file {path} | {line.TrimStart(' ')}";
            }

            return $"{padding}{this.Id} {this.Message}";
        }

        /// <summary>
        /// Check if position matches if specified.
        /// </summary>
        /// <param name="location">The actual diagnostic.</param>
        /// <returns>True if match.</returns>
        private bool PositionMatches(Location location)
        {
            if (!this.HasPosition)
            {
                return true;
            }

            var actualSpan = location.GetMappedLineSpan();
            if (this.Span.StartLinePosition != actualSpan.StartLinePosition)
            {
                return false;
            }

            if (this.HasPath &&
                this.Span.Path != actualSpan.Path)
            {
                return false;
            }

            if (this.Span.StartLinePosition != this.Span.EndLinePosition)
            {
                return this.Span.EndLinePosition == actualSpan.EndLinePosition;
            }

            return true;
        }
    }
}
