﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Ruminoid.Toolbox.Core;
using Ruminoid.Toolbox.Formatting;
using Ruminoid.Toolbox.Utils;

namespace Ruminoid.Toolbox.Composition
{
    [Export]
    public class PluginHelper
    {
        public PluginHelper(ILogger<PluginHelper> logger)
        {
            _logger = logger;

            Initialize();
        }

        #region Plugin Loader

        private void Initialize()
        {
            _logger.LogDebug("Starting collecting plugins.");

            string pluginsFolderPath = StorageHelper.GetSectionFolderPath("plugins");

            string[] files = Directory.GetFiles(
                pluginsFolderPath,
                "*.dll",
                SearchOption.AllDirectories)
                .Where(x =>
                    (Path.GetFileNameWithoutExtension(x).StartsWith("rmbox-plugin-") ||
                     Path.GetFileNameWithoutExtension(x).StartsWith("Ruminoid.Toolbox.Plugin.")) &&
                    x.EndsWith(".dll") ||
                    x.EndsWith(".plugin.dll"))
                .Where(x => !x.Replace(pluginsFolderPath, "")
                    .Contains($"ref{Path.DirectorySeparatorChar}"))
                .ToArray();

            _logger.LogDebug($"Collected {files.Length} plugin(s).");

            if (files.Length == 0)
            {
                _logger.LogInformation("没有发现插件。");
                return;
            }

            _logger.LogDebug("Loading plugins.");
            
            foreach (string file in files)
            {
                try
                {
                    _logger.LogDebug($"Loading plugin: {file}");

                    Assembly pluginAssembly = LoadPlugin(file);
                    Type[] exportedTypes = pluginAssembly.GetExportedTypes();

                    string pluginName = pluginAssembly.FullName;

                    _logger.LogDebug($"Parsing meta in plugin: {pluginName}");

                    Type[] metaTypes = exportedTypes.Where(type =>
                        Attribute.GetCustomAttribute(type, typeof(RmboxPluginAttribute)) is not null).ToArray();

                    if (metaTypes.Length != 1)
                    {
                        string err = $"插件 {pluginName} 具有的元信息的个数 {metaTypes.Length} 不符合 1 的要求。";
                        _logger.LogError(err);
                        throw new IndexOutOfRangeException(err);
                    }

                    // ReSharper disable once UseNegatedPatternMatching
                    IMeta pluginMeta = Activator.CreateInstance(metaTypes.Single()) as IMeta;
                    if (pluginMeta is null)
                    {
                        string err = $"在加载插件 {pluginName} 的元信息时发生错误。";
                        _logger.LogError(err);
                        throw new ArgumentNullException(nameof(pluginMeta));
                    }

                    _logger.LogDebug($"Meta parsed in: {pluginMeta.Name}");

                    MetaCollection.Add(new Tuple<IMeta, Assembly>(pluginMeta, pluginAssembly));

                    _logger.LogDebug($"Parsing components in plugin: {pluginMeta.Name}");

                    List<Type> operationTypes = exportedTypes.Where(type =>
                        Attribute.GetCustomAttribute(type, typeof(OperationAttribute)) is not null).ToList();

                    foreach (Type t in operationTypes.Where(type => !type.IsAssignableTo(typeof(IOperation)))
                        .ToArray())
                    {
                        _logger.LogWarning($"检测到错误导出的类型 {t.FullName}，将会忽略加载。");
                        operationTypes.Remove(t);
                    }

                    foreach (Type operationType in operationTypes)
                    {
                        OperationAttribute operationAttribute =
                            Attribute.GetCustomAttribute(operationType, typeof(OperationAttribute))
                            as OperationAttribute;

                        OperationCollection.Add(new Tuple<OperationAttribute, Type>(
                            operationAttribute,
                            operationType));

                        _logger.LogDebug($"Operation {operationAttribute.Name} loaded.");
                    }

                    List<Type> configSectionTypes = exportedTypes.Where(type =>
                        Attribute.GetCustomAttribute(type, typeof(ConfigSectionAttribute)) is not null).ToList();

                    foreach (Type t in configSectionTypes.Where(type => !type.IsSubclassOf(typeof(ConfigSectionBase)))
                        .ToArray())
                    {
                        _logger.LogWarning($"检测到错误导出的类型 {t.FullName}，将会忽略加载。");
                        configSectionTypes.Remove(t);
                    }

                    foreach (Type configSectionType in configSectionTypes)
                    {
                        var configSectionAttribute =
                            Attribute.GetCustomAttribute(configSectionType, typeof(ConfigSectionAttribute))
                            as ConfigSectionAttribute;

                        ConfigSectionCollection.Add(new Tuple<ConfigSectionAttribute, Type>(
                            configSectionAttribute,
                            configSectionType));

                        _logger.LogDebug($"Config section {configSectionAttribute.Id} loaded.");
                    }

                    List<Type> formatterTypes = exportedTypes.Where(type =>
                        Attribute.GetCustomAttribute(type, typeof(FormatterAttribute)) is not null).ToList();

                    foreach (Type t in formatterTypes.Where(type => !type.IsAssignableTo(typeof(IFormatter)))
                        .ToArray())
                    {
                        _logger.LogWarning($"检测到错误导出的类型 {t.FullName}，将会忽略加载。");
                        formatterTypes.Remove(t);
                    }

                    foreach (Type formatterType in formatterTypes)
                    {
                        FormatterAttribute formatterAttribute =
                            Attribute.GetCustomAttribute(formatterType, typeof(FormatterAttribute))
                            as FormatterAttribute;

                        FormatterCollection.Add(new Tuple<FormatterAttribute, Type>(
                            formatterAttribute,
                            formatterType));

                        _logger.LogDebug($"Formatter {formatterType.FullName} loaded.");
                    }

                    _logger.LogDebug($"Plugin {pluginMeta.Name} loaded.");
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, $"在加载插件 {file} 时发生错误。将跳过该插件的加载。");
                }
            }

            _logger.LogInformation(
                $"加载了 {MetaCollection.Count} 个插件共 {OperationCollection.Count + ConfigSectionCollection.Count + FormatterCollection.Count} 个组件。");
        }

        private static Assembly LoadPlugin(string path)
        {
            PluginLoadContext loadContext = new PluginLoadContext(path);
            return loadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(path)));
        }

        #endregion

        #region Utils

        public Collection<Tuple<IMeta, Assembly>> MetaCollection { get; } = new();

        public Collection<Tuple<OperationAttribute, Type>> OperationCollection { get; } = new();

        private Dictionary<string, Tuple<OperationAttribute, IOperation>> OperationCache { get; } = new();

        public Collection<Tuple<ConfigSectionAttribute, Type>> ConfigSectionCollection { get; } = new();

        public Collection<Tuple<FormatterAttribute, Type>> FormatterCollection { get; } = new();

        private Dictionary<string, Tuple<FormatterAttribute, IFormatter>> FormatterCache { get; } = new();

        public Tuple<OperationAttribute, IOperation> GetOperation(string id)
        {
            bool success = OperationCache.TryGetValue(id, out Tuple<OperationAttribute, IOperation> cached);
            if (success) return cached;

            Tuple<OperationAttribute, Type> tuple = OperationCollection.FirstOrDefault(x => x.Item1.Id == id);

            // ReSharper disable once InvertIf
            if (tuple is null)
            {
                string err = $"找不到 ID 为 {id} 的操作。可能需要安装相关的插件以解决此问题。";
                _logger.LogError(err);
                throw new PluginCompositionException(err);
            }

            Tuple<OperationAttribute, IOperation> created =
                new(tuple.Item1, Activator.CreateInstance(tuple.Item2) as IOperation);

            OperationCache.TryAdd(tuple.Item1.Id, created);

            return created;
        }

        public Tuple<ConfigSectionAttribute, ConfigSectionBase> CreateConfigSection(string id)
        {
            Tuple<ConfigSectionAttribute, Type> tuple = ConfigSectionCollection.FirstOrDefault(x => x.Item1.Id == id);

            // ReSharper disable once InvertIf
            if (tuple is null)
            {
                string err = $"找不到 ID 为 {id} 的配置项。可能需要安装相关的插件以解决此问题。";
                _logger.LogError(err);
                throw new PluginCompositionException(err);
            }

            return new Tuple<ConfigSectionAttribute, ConfigSectionBase>(tuple.Item1,
                Activator.CreateInstance(tuple.Item2) as ConfigSectionBase);
        }

        public Tuple<FormatterAttribute, IFormatter> GetFormatter(string target)
        {
            bool success = FormatterCache.TryGetValue(target, out Tuple<FormatterAttribute, IFormatter> cached);
            if (success) return cached;

            Tuple<FormatterAttribute, Type> tuple =
                FormatterCollection.FirstOrDefault(x => x.Item1.Targets.Split('|').Contains(target));

            // ReSharper disable once InvertIf
            if (tuple is null)
            {
                string err = $"找不到目标为 {target} 的格式器。可能需要安装相关的插件以解决此问题。";
                _logger.LogError(err);
                throw new PluginCompositionException(err);
            }

            Tuple<FormatterAttribute, IFormatter> created =
                new(tuple.Item1, Activator.CreateInstance(tuple.Item2) as IFormatter);

            foreach (string s in tuple.Item1.Targets.Split('|'))
                FormatterCache.TryAdd(s, created);

            return created;
        }

        #endregion

        private readonly ILogger<PluginHelper> _logger;
    }
}
