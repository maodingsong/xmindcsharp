﻿//TODO: cyclic dependency with XMindAPI.Configuration and XMindAPI.Writers.Configuraiton;
using System;

using System.Collections.Generic;

using System.Linq;
using System.Xml.Linq;

using XMindAPI.Configuration;
using XMindAPI.Writers;
using XMindAPI.Core;
using XMindAPI.Core.Builders;
using XMindAPI.Core.DOM;
using static XMindAPI.Core.DOM.DOMConstants;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace XMindAPI.Models
{
    /// <summary>
    /// XMindWorkBook encapsulates an XMind workbook and methods for performing actions on workbook content.
    /// </summary>
    public class XMindWorkBook : AbstractWorkbook, INodeAdaptableFactory//IWorkbook
    {
        public string Name { get; set; }
        private readonly XMindConfiguration _bookConfiguration;
        private readonly IXMindDocumentBuilder _documentBuilder;

        private readonly NodeAdaptableRegistry _adaptableRegistry;
        internal readonly IConfiguration _xMindSettings;

        private readonly XElement _implementation;

        // private string _fileName = null;

        /// <summary>
        /// Creates a new XMind workbook if loadContent is false, otherwise the file content will be loaded.
        /// </summary>
        // /// <param name="loadContent">If true, the current data from the file will be loaded, otherwise an empty workbook will be created.</param>
        internal XMindWorkBook(string name, XMindConfiguration bookConfiguration, IXMindDocumentBuilder builder)
        {
            Name = name;
            _xMindSettings = XMindConfigurationLoader.Configuration.XMindConfigCollection;
            this._bookConfiguration = bookConfiguration;
            _documentBuilder = builder;

            _documentBuilder.CreateMetaFile();
            _documentBuilder.CreateManifestFile();
            _documentBuilder.CreateContentFile();

            _implementation = _documentBuilder.ContentFile.Descendants().First();
            _adaptableRegistry = new NodeAdaptableRegistry(_documentBuilder.ContentFile, this);
            //Create default sheet if needed
            //TODO:
            if (DOMUtils.GetFirstElementByTagName(_implementation, TAG_SHEET) == null)
            {
                AddSheet(CreateSheet());
            }

        }

        public T GetAdapter<T>(Type adapter)
        {
            //TODO: this is point of extension for all adaptees
            // if (IStorage.class.equals(adapter))
            //     return adapter.cast(getStorage());
            // if (IEntryStreamNormalizer.class.equals(adapter))
            //     return adapter.cast(manifest.getStreamNormalizer());
            // if (ICoreEventSource.class.equals(adapter))
            //     return adapter.cast(this);
            // if (adapter.isAssignableFrom(Document.class))
            //     return adapter.cast(implementation);
            // if (adapter.isAssignableFrom(Element.class))
            //     return adapter.cast(getWorkbookElement());
            // if (IMarkerSheet.class.equals(adapter))
            //     return adapter.cast(getMarkerSheet());
            // if (IManifest.class.equals(adapter))
            //     return adapter.cast(getManifest());
            // if (ICoreEventSupport.class.equals(adapter))
            //     return adapter.cast(getCoreEventSupport());
            // if (INodeAdaptableFactory.class.equals(adapter))
            //     return adapter.cast(this);
            // if (INodeAdaptableProvider.class.equals(adapter))
            //     return adapter.cast(getAdaptableRegistry());
            // if (IMarkerRefCounter.class.equals(adapter))
            //     return adapter.cast(getMarkerRefCounter());
            // if (IStyleRefCounter.class.equals(adapter))
            //     return adapter.cast(getStyleRefCounter());
            // if (IWorkbookComponentRefManager.class.equals(adapter))
            //     return adapter.cast(getElementRefCounter());
            // if (IRevisionRepository.class.equals(adapter))
            //     return adapter.cast(getRevisionRepository());
            // if (IWorkbookExtensionManager.class.equals(adapter))
            //     return adapter.cast(getWorkbookExtensionManager());
            return base.GetAdapter<T>(adapter);
        }

        /// <summary>
        /// Save the current XMind workbook file to disk.
        /// </summary>
        public override async Task Save()
        {
            var manifestFileName = _xMindSettings[XMindConfiguration.ManifestLabel];
            var metaFileName = _xMindSettings[XMindConfiguration.MetaLabel];
            var contentFileName = _xMindSettings[XMindConfiguration.ContentLabel];

            var files = new Dictionary<string, XDocument>(3)
            {
                [metaFileName] = _documentBuilder.MetaFile,
                [manifestFileName] = _documentBuilder.ManifestFile,
                [contentFileName] = _documentBuilder.ContentFile
            };

            var writerContexts = new List<XMindWriterContext>();
            foreach (var kvp in files)
            {
                var currentWriterContext = new XMindWriterContext()
                {
                    FileName = kvp.Key,
                    FileEntries = new XDocument[1] { kvp.Value }
                };
                var selectedWriters = _bookConfiguration
                    .WriteTo
                    .ResolveWriters(currentWriterContext);
                if (selectedWriters == null)
                {
                    throw new InvalidOperationException("XMindBook.Save: Writer is not selected");
                }

                foreach (var writer in selectedWriters)
                {
                    writer.WriteToStorage(kvp.Value, kvp.Key);
                }
                writerContexts.Add(currentWriterContext);
            }
            _bookConfiguration.WriteTo.FinalizeAction?.Invoke(writerContexts, this);
        }

        public override IRelationship CreateRelationship(IRelationship rel1, IRelationship rel2)
        {
            throw new NotImplementedException();
        }

        public override IRelationship CreateRelationship()
        {
            throw new NotImplementedException();
        }

        public override ISheet CreateSheet()
        {
            var sheetElement = new XElement(TAG_SHEET);
            // GetWorkbookElement().Add(sheetElement);
            XMindSheet sheet = new XMindSheet(sheetElement, this);
            _adaptableRegistry.RegisterByNode(sheet, sheet.Implementation);
            return sheet;
        }

        public override void AddSheet(ISheet sheet, int index)
        {
            XElement elementImplementation = (sheet as XMindSheet)?.Implementation;
            var bookImplementation = GetWorkbookElement();
            if (elementImplementation == null)
            {
                // Logger.Warn("XMindWorkbook.AddSheet: sheet is not correct");
                return;
            }
            // if (elementImplementation.Parent != bookImplementation)
            // {
            //     Logger.Warn("XMindWorkbook.AddSheet: sheet must belong to same document");
            // }
            var childElements = DOMUtils.GetChildElementsByTag(bookImplementation, TAG_SHEET);
            if (index >= 0 && index < childElements.Count())
            {
                childElements.Where((e, i) => i == index)
                    .First()
                    .AddBeforeSelf(elementImplementation);
            }
            else
            {
                bookImplementation.Add(elementImplementation);
            }
        }

        public override ITopic CreateTopic()
        {
            var topicElement = new XElement(TAG_TOPIC);
            // GetWorkbookElement().Add(topicElement);
            XMindTopic topic = new XMindTopic(topicElement, this);
            _adaptableRegistry.RegisterByNode(topic, topic.Implementation);
            return topic;
        }

        public override object FindElement(string id, IAdaptable source)
        {
            XNode node = source.GetAdapter<XNode>(typeof(XNode));
            if (node == null)
            {
                node = this.GetWorkbookElement();
            }
            return GetAdaptableRegistry()
                .GetAdaptable(id, node.Document);
        }

        public override ISheet GetPrimarySheet()
        {
            XElement primarySheet = DOMUtils.GetFirstElementByTagName(GetWorkbookElement(), TAG_SHEET);
            if (primarySheet != null)
            {
                return (ISheet)GetAdaptableRegistry().GetAdaptable(primarySheet);
            }
            return null;
        }

        public override IEnumerable<ISheet> GetSheets()
        {
            return DOMUtils.GetChildList<ISheet>(GetWorkbookElement(), TAG_SHEET, GetAdaptableRegistry());
        }
        public override void RemoveSheet(ISheet sheet)
        {
            XElement elementImplementation = (sheet as XMindSheet)?.Implementation;
            var bookImplementation = GetWorkbookElement();
            if (elementImplementation == null)
            {
                // Logger.Warn("XMindWorkbook.RemoveSheet: sheet is not correct");
                return;
            }
            if (elementImplementation.Parent != bookImplementation)
            {
                // Logger.Warn("XMindWorkbook.RemoveSheet: sheet must belong to same document");
            }
            var childElements = DOMUtils
                .GetChildElementsByTag(bookImplementation, TAG_SHEET).ToList();
            childElements
                .FirstOrDefault(el => el == elementImplementation)?
                .Remove();
        }
        public IAdaptable CreateAdaptable(XNode node)
        {
            IAdaptable a = null;
            if (node is XElement)
            {
                XElement e = (XElement)node;
                XName nodeName = e.Name;
                switch (nodeName.ToString())
                {
                    case TAG_SHEET:
                        a = new XMindSheet(e, this);
                        break;
                    case TAG_TOPIC:
                        a = new XMindTopic(e, this);
                        break;
                }
            }
            if (a != null)
            {
                // Logger.Info($"XMindWorkbook.CreateAdaptable: adaptable is created - {a}");
            }
            else
            {
                // Logger.Warn($"XMindWorkbook.CreateAdaptable: adaptable was is not created - {a}");
            }
            return a;
        }

        // public override string ToString()
        // {
        //     return $"Workbook# {_globalConfiguration.WorkbookName}";
        // }

        internal NodeAdaptableRegistry GetAdaptableRegistry()
        {
            return _adaptableRegistry;
        }

        internal XElement GetWorkbookElement()
        {
            return this._implementation;
        }
    }
}
