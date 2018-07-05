// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace ICSharpCode.TextEditor.Document
{
    public class HighlightingManager
    {
        private readonly ArrayList syntaxModeFileProviders = new ArrayList();

        // hash table from extension name to highlighting definition,
        // OR from extension name to Pair SyntaxMode,ISyntaxModeFileProvider

        private readonly Hashtable extensionsToName = new Hashtable();

        public Hashtable HighlightingDefinitions { get; } = new Hashtable();

        public static HighlightingManager Manager { get; }

        static HighlightingManager()
        {
            Manager = new HighlightingManager();
            Manager.AddSyntaxModeFileProvider(new ResourceSyntaxModeProvider());
        }

        public HighlightingManager()
        {
            CreateDefaultHighlightingStrategy();
        }

        public void AddSyntaxModeFileProvider(ISyntaxModeFileProvider syntaxModeFileProvider)
        {
            foreach (SyntaxMode syntaxMode in syntaxModeFileProvider.SyntaxModes) {
                HighlightingDefinitions[syntaxMode.Name] = new DictionaryEntry(syntaxMode, syntaxModeFileProvider);
                foreach (string extension in syntaxMode.Extensions) {
                    extensionsToName[extension.ToUpperInvariant()] = syntaxMode.Name;
                }
            }
            if (!syntaxModeFileProviders.Contains(syntaxModeFileProvider)) {
                syntaxModeFileProviders.Add(syntaxModeFileProvider);
            }
        }

        public void AddHighlightingStrategy(IHighlightingStrategy highlightingStrategy)
        {
            HighlightingDefinitions[highlightingStrategy.Name] = highlightingStrategy;
            foreach (string extension in highlightingStrategy.Extensions)
            {
                extensionsToName[extension.ToUpperInvariant()] = highlightingStrategy.Name;
            }
        }

        public void ReloadSyntaxModes()
        {
            HighlightingDefinitions.Clear();
            extensionsToName.Clear();
            CreateDefaultHighlightingStrategy();
            foreach (ISyntaxModeFileProvider provider in syntaxModeFileProviders) {
                provider.UpdateSyntaxModeList();
                AddSyntaxModeFileProvider(provider);
            }
            OnReloadSyntaxHighlighting(EventArgs.Empty);
        }

        private void CreateDefaultHighlightingStrategy()
        {
            DefaultHighlightingStrategy defaultHighlightingStrategy = new DefaultHighlightingStrategy();
            defaultHighlightingStrategy.Extensions = new string[] {};
            defaultHighlightingStrategy.Rules.Add(new HighlightRuleSet());
            HighlightingDefinitions["Default"] = defaultHighlightingStrategy;
        }

        private IHighlightingStrategy LoadDefinition(DictionaryEntry entry)
        {
            SyntaxMode              syntaxMode             = (SyntaxMode)entry.Key;
            ISyntaxModeFileProvider syntaxModeFileProvider = (ISyntaxModeFileProvider)entry.Value;

            DefaultHighlightingStrategy highlightingStrategy = null;
            try {
                var reader = syntaxModeFileProvider.GetSyntaxModeFile(syntaxMode);
                if (reader == null)
                    throw new HighlightingDefinitionInvalidException("Could not get syntax mode file for " + syntaxMode.Name);
                highlightingStrategy = HighlightingDefinitionParser.Parse(syntaxMode, reader);
                if (highlightingStrategy.Name != syntaxMode.Name) {
                    throw new HighlightingDefinitionInvalidException("The name specified in the .xshd '" + highlightingStrategy.Name + "' must be equal the syntax mode name '" + syntaxMode.Name + "'");
                }
            } finally {
                if (highlightingStrategy == null) {
                    highlightingStrategy = DefaultHighlighting;
                }
                HighlightingDefinitions[syntaxMode.Name] = highlightingStrategy;
                highlightingStrategy.ResolveReferences();
            }
            return highlightingStrategy;
        }

        public DefaultHighlightingStrategy DefaultHighlighting => (DefaultHighlightingStrategy)HighlightingDefinitions["Default"];

        internal KeyValuePair<SyntaxMode, ISyntaxModeFileProvider> FindHighlighterEntry(string name)
        {
            foreach (ISyntaxModeFileProvider provider in syntaxModeFileProviders) {
                foreach (SyntaxMode mode in provider.SyntaxModes) {
                    if (mode.Name == name) {
                        return new KeyValuePair<SyntaxMode, ISyntaxModeFileProvider>(mode, provider);
                    }
                }
            }
            return new KeyValuePair<SyntaxMode, ISyntaxModeFileProvider>(null, null);
        }

        public IHighlightingStrategy FindHighlighter(string name)
        {
            object def = HighlightingDefinitions[name];
            if (def is DictionaryEntry entry) {
                return LoadDefinition(entry);
            }
            return def == null ? DefaultHighlighting : (IHighlightingStrategy)def;
        }

        public IHighlightingStrategy FindHighlighterForFile(string fileName)
        {
            string highlighterName = (string)extensionsToName[Path.GetExtension(fileName).ToUpperInvariant()];
            if (highlighterName != null) {
                object def = HighlightingDefinitions[highlighterName];
                if (def is DictionaryEntry entry) {
                    return LoadDefinition(entry);
                }
                return def == null ? DefaultHighlighting : (IHighlightingStrategy)def;
            }

            return DefaultHighlighting;
        }

        protected virtual void OnReloadSyntaxHighlighting(EventArgs e)
        {
            ReloadSyntaxHighlighting?.Invoke(this, e);
        }

        public event EventHandler ReloadSyntaxHighlighting;
    }
}
