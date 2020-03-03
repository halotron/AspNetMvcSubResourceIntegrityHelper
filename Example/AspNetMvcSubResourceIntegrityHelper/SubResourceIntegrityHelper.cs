using AspNetMvcSubResourceIntegrityHelper.Config;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AspNetMvcSubResourceIntegrityHelper
{
	public static class SubResourceIntegrityHelper
	{
		private static Dictionary<string, string> _dictionary = new Dictionary<string, string>();

		public static IHtmlString SRIResourceWithTags(this HtmlHelper helper, string linkWithTags)
		{
			if (_dictionary.ContainsKey(linkWithTags))
			{
				var foo = _dictionary[linkWithTags];
				if (foo != "local")
				{
					return new HtmlString(_dictionary[linkWithTags]);
				}
				else
				{
					return new HtmlString(linkWithTags);
				}
			}

			if (string.IsNullOrWhiteSpace(linkWithTags))
			{
				return new HtmlString(string.Empty);
			}
			if (ConfigHelper.SriThrowIfUsed)
			{
				throw new Exception("SubResourceIntegrityHelper is in use for link: " + linkWithTags);
			}

			var filePath = ConfigHelper.SriFilePath;
			if (filePath != null && !filePath.Contains(Path.DirectorySeparatorChar))
			{
				try
				{
					var p = helper.ViewContext.Controller.ControllerContext.HttpContext.Server.MapPath("/");
					var localPath = Path.Combine(p, filePath);
					if (!File.Exists(localPath))
					{
						filePath = null;
					}
					else
					{
						filePath = localPath;
					}
				}
				catch (Exception)
				{
					try
					{
						var loc = typeof(SubResourceIntegrityHelper).Assembly.Location;
						var p = Path.GetDirectoryName(loc);
						var localPath = Path.Combine(p, filePath);
						if (!File.Exists(localPath))
						{
							filePath = null;
						}
						else
						{
							filePath = localPath;
						}
					}
					catch (Exception)
					{
						filePath = null;
					}
				}
			}

			if (filePath != null)
			{
				if (File.Exists(filePath))
				{
					var lines = File.ReadAllLines(filePath);
					foreach (var line in lines)
					{
						if (!string.IsNullOrWhiteSpace(line) && line.Contains("\t"))
						{
							var parts = line.Split('\t');
							if (parts.Length == 2)
							{
								if (!_dictionary.ContainsKey(parts[0]))
								{
									_dictionary.Add(parts[0], parts[1]);
								}
							}
						}
					}
				}
				else
				{
					try
					{
						if (filePath != null)
						{
							using (var fs = File.Create(filePath))
							{
							}
						}
					}
					catch (Exception)
					{
						return new HtmlString(
							"<script src=\"CAN NOT WRITE TO SubResourceIntegrityHelper_FilePath\" />");
					}
				}

				if (_dictionary.ContainsKey(linkWithTags))
				{
					return new HtmlString(_dictionary[linkWithTags]);
				}

                if (ConfigHelper.SriDownloadIfNotPresentInFile)
				{
					var r = Regex.Match(linkWithTags, ".*(<script|<link){1}.*(src=\"|href=\"){1}([^\"]*)(.*)",
						RegexOptions.IgnoreCase);
					if (r.Groups.Count == 5)
					{
						var isScript = string.Compare(r.Groups[1].Value, "<script",
										   StringComparison.InvariantCultureIgnoreCase) == 0;
						var uriInLink = r.Groups[3].Value;
						if (!string.IsNullOrWhiteSpace(uriInLink))
						{
							string uriToGet = null;
							if (!(CultureInfo.CurrentCulture.CompareInfo.IndexOf(uriInLink, "integrity=\"", CompareOptions.IgnoreCase) >= 0))
							{
								bool isLocalUri = !uriInLink.StartsWith("http", true, CultureInfo.CurrentCulture);
								if (isLocalUri)
								{
									var basePath = ConfigHelper.SriInsteadOfTilde;
									if (string.IsNullOrWhiteSpace(basePath))
									{
										if (uriInLink.StartsWith("~"))
										{
											uriInLink = System.Web.VirtualPathUtility.ToAbsolute(uriInLink);
											if (isScript)
											{
												return SRIScriptLink(helper, uriInLink);
											}
											else
											{
												return SRICssLink(helper, uriInLink);
											}
										}
										if (!_dictionary.ContainsKey(linkWithTags))
										{
											_dictionary.Add(linkWithTags,
												"local"); //vi undviker att spara till fil eftersom lokal sökväg inte bör existera annat än i utveckling
										}
										return new HtmlString(linkWithTags);
									}
									if (uriInLink.StartsWith("~"))
									{
										uriInLink = uriInLink.Substring(1, uriInLink.Length - 1);
									}
									if (!basePath.EndsWith("/"))
									{
										basePath = basePath + "/";
									}
									if (uriInLink.StartsWith("/"))
									{
										uriInLink = uriInLink.Substring(1, uriInLink.Length - 1);
									}
									uriToGet = basePath + uriInLink;
								}
								else
								{
									uriToGet = uriInLink;
								}
								bool success = false;
								byte[] bytes = null;
								using (var client = new WebClient())
								{
									try
									{
										bytes = client.DownloadData(uriToGet);
										success = true;
									}
									catch (Exception)
									{
										success = false;
									}
								}
								if (success)
								{
									byte[] result = null;
									using (SHA384 shaM = new SHA384Managed())
									{
										result = shaM.ComputeHash(bytes);
										var hashBase64 = Convert.ToBase64String(result);
										string pre, post;
										if (linkWithTags.Contains("/>"))
										{
											var index = linkWithTags.LastIndexOf(">");
											index = index - 1;
											pre = linkWithTags.Substring(0, index);
											post = "/>";
										}
										else
										{
											var index = linkWithTags.IndexOf(">");
											pre = linkWithTags.Substring(0, index);
											post = linkWithTags.Substring(index);
										}
										StringBuilder sb = new StringBuilder();
										sb.Append(pre);
										sb.Append(" ");
										sb.Append("integrity=\"");
										sb.Append("sha384-");
										sb.Append(hashBase64);
										sb.Append("\"");
										if (!linkWithTags.Contains("crossorigin=\""))
										{
											sb.Append(" crossorigin=\"anonymous\"");
										}
										sb.Append(" ");
										sb.Append(post);

										var theNewLink = sb.ToString();

										if (!_dictionary.ContainsKey(linkWithTags))
										{
											try
											{
												using (var fs = File.Open(filePath, FileMode.Append))
												{
													using (var sw = new StreamWriter(fs))
													{
														sw.WriteLine(linkWithTags + "\t" + theNewLink);
													}
												}
												_dictionary.Add(linkWithTags, theNewLink);
											}
											catch (Exception)
											{
												return new HtmlString(
													"<script src=\"CAN NOT WRITE TO SubResourceIntegrityHelper_FilePath\" />");
											}
										}
										return new HtmlString(theNewLink);
									}
								}
							}
						}
					}
				}
			}
			return new HtmlString(linkWithTags);
		}

		public static IHtmlString SRICssLink(this HtmlHelper helper, string link)
		{
			return SRIResourceWithTags(helper, "<link href=\"" + link + "\" rel=\"stylesheet\" />");
		}

		public static IHtmlString SRIScriptLink(this HtmlHelper helper, string link)
		{
			return SRIResourceWithTags(helper, "<script src=\"" + link + "\" ></script>");
		}

        public static IHtmlString SRIWrapScriptsRender(this HtmlHelper helper, IHtmlString htmlString)
        {
            return DoEachRow(helper, htmlString, "<script ");
        }


        public static IHtmlString SRIWrapStylesRender(this HtmlHelper helper, IHtmlString htmlString)
        {
            return DoEachRow(helper, htmlString, "<link href=\"");
        }

        private static IHtmlString DoEachRow(HtmlHelper helper, IHtmlString htmlString, string firstStartsWith)
        {
            var str = htmlString.ToHtmlString();
            StringBuilder sb = new StringBuilder();
            if (str.StartsWith(firstStartsWith))
            {
                if (str.Contains("\n"))
                {

                    foreach (var rad in str.Split('\n'))
                    {
                        sb.Append(SRIResourceWithTags(helper, rad.Trim()));
                    }
                    return new HtmlString(sb.ToString());
                }
                else
                {
                    var delar = str.Split(new[] { "/>" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var del in delar)
                    {
                        sb.Append(SRIResourceWithTags(helper, del.Trim()));
                    }
                    return new HtmlString(sb.ToString());
                }
            }
            return htmlString;
        }
    }
}
