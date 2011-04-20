using System;
using System.Collections.Generic;
using System.Configuration;
using FluentNHibernate.Automapping;
using FluentNHibernate.Cfg;
using FluentNHibernate.Cfg.Db;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;

namespace FluentNHibernateSimpleConfiguration
{
	public class Options
	{
		public string ConnectionString { get; set; }
		public string NamespaceToScan { get; set; }
		public Func<AutoPersistenceModel,AutoPersistenceModel> Overrider { get; set; }
		public bool CreateDB { get; set; }
		public IPersistenceConfigurer PersistenceConfigurer { get; set; }
		public bool ShowSql { get; set; }
		public DefaultAutomappingConfiguration DefaultAutomappingConfiguration { get; set; }
		public Dictionary<string, string> ConfigProperties;

		public Options()
		{
			ConfigProperties = new Dictionary<string, string>();
			Overrider = model => model;
			ShowSql = false;
			CreateDB = false;
		}
	}

	public class Setup
	{
		public static ISessionFactory SessionFactory { get; private set; }
		protected static FluentConfiguration FluentConfiguration;
		public static Options Options { get; set; }
		//public delegate void ExposeConfigurationDelegate(Configuration model);

		/// <summary>
		/// Initializes the DB.
		/// </summary>
		/// <typeparam name="T">Scan assembly of type T for clases to automap</typeparam>
		/// <param name="options"></param>
		public static void InitializeDB<T>(Options options)
		{
			Options = options;

			InitializeOptions();

			Action<MappingConfiguration> mappings =
				m => m.AutoMappings.Add(
					Options.Overrider(AutoMap.AssemblyOf<T>(Options.DefaultAutomappingConfiguration)));

			FluentConfiguration = Fluently.Configure()
				.Database(Options.PersistenceConfigurer)
				.Mappings(mappings);

			SessionFactory = FluentConfiguration
								.ExposeConfiguration(ExposeConfigurationSetup)
								.BuildSessionFactory();
		}


		private static void InitializeOptions()
		{
			if (Options.DefaultAutomappingConfiguration == null)
				Options.DefaultAutomappingConfiguration = new DefaultNHConfiguration();

			if (Options.PersistenceConfigurer == null)
			{
				var mssqlconf = MsSqlConfiguration.MsSql2005;

				if (Options.ShowSql)
					mssqlconf.ShowSql();

				if (string.IsNullOrEmpty(Options.ConnectionString))
					throw new Exception("ConnectionString missing");

				Options.PersistenceConfigurer =
					mssqlconf.ConnectionString(c => c.FromConnectionStringWithKey(Options.ConnectionString));
			}

			if (!Options.ConfigProperties.ContainsKey("current_session_context_class"))
				Options.ConfigProperties["Options.ConfigProperties"] = "web";

			if (string.IsNullOrEmpty(Options.NamespaceToScan))
				throw new Exception("NamespaceToScan missing");

		}

		public static void ExposeConfigurationSetup(Configuration cfg)
		{
			if (Options.CreateDB)
				new SchemaExport(cfg).Create(false, true);

			foreach (var property in Options.ConfigProperties)
				cfg.SetProperty(property.Key, property.Value);

		}

		public class DefaultNHConfiguration : DefaultAutomappingConfiguration
		{
			public override bool ShouldMap(Type type)
			{
				return type.Namespace == Options.NamespaceToScan;
			}

			//public override bool IsComponent(Type type)
			//{
			//    throw new NotImplementedException();
			//}

			//public override bool IsId(Member member)
			//{
			//    throw new NotImplementedException();
			//}
		}

	}
}
