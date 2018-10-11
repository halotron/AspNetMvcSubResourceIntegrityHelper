using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace AspNetMvcSubResourceIntegrityHelper.Config
{
	public static class ConfigHelper
	{
		public static bool SriThrowIfUsed => string.Compare(ConfigurationManager.AppSettings["SriHelper_ThrowIfUsed"], "true",
												 StringComparison.InvariantCultureIgnoreCase) == 0;

		public static string SriFilePath => ConfigurationManager.AppSettings["SriHelper_FilePath"];
		public static bool SriDownloadIfNotPresentInFile => string.Compare(ConfigurationManager.AppSettings["SriHelper_DownloadIfNotFoundInFile"], "true",
												 StringComparison.InvariantCultureIgnoreCase) == 0;

		public static string SriInsteadOfTilde => ConfigurationManager.AppSettings["SriHelper_BasePath"];
	}
}
