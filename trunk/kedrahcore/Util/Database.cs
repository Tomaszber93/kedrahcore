﻿using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Linq;
using System.Xml;

namespace Kedrah.Util {
    public class Database {
        public static string filename = "Monsters.xml";
        public int pdone = -1;

        public void CreaturesToFile() {
            string url = "http://tibia.wikia.com/index.php?title=List_of_Creatures&printable=yes";

            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);

            request.BeginGetResponse(delegate(IAsyncResult ar) {
                string html = Tibia.Website.GetHTML(ar);

                pdone = 0;

                CreaturesToFile(html);
            }, request);
        }

        public void CreaturesToFile(string html) {
            try {
                string url;
                XmlDocument doc = new XmlDocument();
                XmlTextWriter xmlWriter = new XmlTextWriter(filename, System.Text.Encoding.UTF8);
                xmlWriter.Formatting = Formatting.Indented;
                xmlWriter.WriteProcessingInstruction("xml", "version='1.0' encoding='UTF-8'");
                xmlWriter.WriteStartElement("monsters");
                xmlWriter.Close();
                XmlTextReader reader = new XmlTextReader(filename);
                doc = new XmlDocument();
                doc.Load(reader);
                reader.Close();

                HttpWebRequest request;

                List<string> names = new List<string>();
                MatchCollection namesMatched = Regex.Matches(html, @"<td><a href=""/wiki/([^<]*)"" title=""", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match nameMatched in namesMatched)
                    names.Add(nameMatched.Groups[1].Value);

                foreach (string name in names) {
                    url = "http://tibia.wikia.com/index.php?title=" + name + "&printable=yes";
                    request = (HttpWebRequest)HttpWebRequest.Create(url);
                    string fragment = "<monster>" +
                    "<name>" + name.Split('%')[0].Trim().Replace('_', ' ').Trim() + "</name>";

                    request.BeginGetResponse(delegate(IAsyncResult ar) {
                        string nhtml = Tibia.Website.GetHTML(ar);

                        CreaturesToFile(nhtml, fragment);
                    }, request);
                }
            }
            catch {

            }
        }

        public void CreaturesToFile(string html, string fragment) {
            try {
                XmlTextReader reader = new XmlTextReader(filename);
                XmlDocument doc = new XmlDocument();
                doc.Load(reader);
                reader.Close();
                XmlNode currNode;
                html = Regex.Replace(html, "<(.|\n)*?>", "");
                html = html.Replace("&nbsp;", "").Replace("&", "").Replace(";", "").Replace("and", ",");
                string weaknesses = Regex.Match(html, @"Weak To:$\n^(.*?)$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Groups[1].Value;
                string strongnesses = Regex.Match(html, @"Strong To:$\n^(.*?)$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Groups[1].Value;
                string immunities = Regex.Match(html, @"Immune To:$\n^(.*?)$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Groups[1].Value;
                string neutral = Regex.Match(html, @"Neutral To:$\n^(.*?)$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Groups[1].Value;
                string abilities = Regex.Match(html, @"Abilities:$\n^(.*?)$", RegexOptions.Multiline | RegexOptions.IgnoreCase).Groups[1].Value;

                XmlDocumentFragment docFrag = doc.CreateDocumentFragment();

                try {
                    foreach (string weakness in weaknesses.Split(',')) {

                        string weak = weakness.Trim();
                        int percent;
                        try {
                            percent = int.Parse(Regex.Match(weak, @"\((.*?)\)", RegexOptions.Singleline | RegexOptions.IgnoreCase).Groups[1].Value.Trim("-+%".ToCharArray()));
                        }
                        catch {
                            percent = -1;
                        }

                        weak = weak.Split(' ')[0].Replace("?", "");
                        if (weak.Length > 0)
                            fragment += "<weakness>" +
                                "<to>" + weak + "</to>" +
                                "<percent>" + percent + "</percent>" +
                                "</weakness>";
                    }
                }
                catch {
                }

                try {
                    foreach (string strongness in strongnesses.Split(',')) {
                        string strong = strongness.Trim();
                        int percent;
                        try {
                            percent = int.Parse(Regex.Match(strong, @"\((.*?)\)", RegexOptions.Singleline | RegexOptions.IgnoreCase).Groups[1].Value.Trim("-+%".ToCharArray()));
                        }
                        catch {
                            percent = -1;
                        }

                        strong = strong.Split(' ')[0].Replace("?", "");
                        if (strong.Length > 1)
                            fragment += "<strongness>" +
                                "<to>" + strong + "</to>" +
                                "<percent>" + percent + "</percent>" +
                                "</strongness>";
                    }
                }
                catch {
                }

                try {
                    foreach (string neutralitie in neutral.Split(',')) {
                        string n = neutralitie.Trim();

                        n = n.Split(' ')[0].Replace("?", "");
                        if (n.Length > 0)
                            fragment += "<neutral>" +
                                "<to>" + n + "</to>" +
                                "</neutral>";
                    }
                }
                catch {
                }

                try {
                    foreach (string immunitie in immunities.Split(',')) {
                        string immune = immunitie.Trim();

                        immune = immune.Split(' ')[0].Replace("?", "");
                        if (immune.Length > 0)
                            fragment += "<immunity>" +
                                "<to>" + immune + "</to>" +
                                "</immunity>";
                    }
                }
                catch {
                }

                if (abilities.ToLower().Contains("wave"))
                    fragment += "<wave>yes</wave>";
                else
                    fragment += "<wave>no</wave>";

                if (abilities.ToLower().Contains("beam"))
                    fragment += "<beam>yes</beam>";
                else
                    fragment += "<beam>no</beam>";

                fragment = fragment.Replace("&nbsp;", "").Replace("&", "").Replace(";", "").Replace(".", "");
                docFrag.InnerXml = fragment + "</monster>";

                currNode = doc.DocumentElement;
                currNode.InsertAfter(docFrag, currNode.LastChild);
                doc.Save(filename);
            }
            catch {

            }
            pdone++;
        }
    }
}
