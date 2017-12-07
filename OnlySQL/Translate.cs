using CSScriptLibrary;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlySQL
{
    public class Translate
    {
        private string CleanWhiteSpace(string source)
        {            
            int length = source.Length;
            var builder = new StringBuilder(length);
            bool prevWhiteSpace = false;

            for (int i = 0; i < length; i++)
            {
                if(char.IsWhiteSpace(source[i]))
                {
                    if (prevWhiteSpace)
                        continue;
                    prevWhiteSpace = true;
                }
                else
                {
                    prevWhiteSpace = false;
                }
                builder.Append(source[i]);
            }

            return builder.ToString();
        }

        private string ParseSQLAndArgs(string source, out string args)
        {
            var length = source.Length;            
            var argBuilder = new StringBuilder();
            var argNameBuilder = new StringBuilder();
            var InArg = false;

            for (int i = 0; i < length; i++)
            {
                if (InArg)
                {
                    if(char.IsLetterOrDigit(source[i]) || source[i] == '.' || source[i] == '_')
                    {
                        argNameBuilder.Append(source[i]);
                    }
                    else
                    {
                        if(argNameBuilder.Length > 0)
                        {
                            var name = argNameBuilder.ToString().Replace(".", "_");
                            source = source.Replace(argNameBuilder.ToString(), name);
                            argBuilder.Append("new MySql.Data.MySqlClient.MySqlParameter(\"@" + name + "\", " + argNameBuilder.ToString() + "),");
                            argNameBuilder = new StringBuilder();
                        }                        
                    }
                }
                else
                {
                    if (source[i] == '@')
                    {
                        InArg = true;
                    }
                }                
            }

            if (argNameBuilder.Length > 0)
            {
                var name = argNameBuilder.ToString().Replace(".", "_");
                source = source.Replace(argNameBuilder.ToString(), name);
                argBuilder.Append("new MySql.Data.MySqlClient.MySqlParameter(\"@" + name + "\", " + argNameBuilder.ToString() + "),");                
            }

            if (argBuilder.Length > 0)
                argBuilder.Length--;

            args = argBuilder.ToString();

            return source;
        }

        private string Parse(string source, out bool HasUsedMysql, out bool HasUsedJson, out bool HasUsedXML)
        {
            if (_trackTime)
            {                
                Console.WriteLine("Started: Parsing Source");
            }

            HasUsedMysql = false;
            HasUsedJson = false;
            HasUsedXML = false;

            //source = CleanWhiteSpace(source);
            var builder = new StringBuilder();
            var InSQLBuilder = new StringBuilder();
            bool inExternalBlock = false;
            bool wasMysql = false;
            bool wasXML = false;
            int startLevel = 0;
            int level = 0;
            string prevWord = "";
            
            foreach (var word in ParseWords(source))
            {
                Console.WriteLine(word);
                string lword = word.ToLower();
                
                if (word.StartsWith("\"") && word.EndsWith("\"") || word.StartsWith("'") && word.EndsWith("'"))
                {
                    if (inExternalBlock)
                    {                                                
                        InSQLBuilder.Append(word);
                    }
                    else
                    {                        
                        builder.Append(word);
                    }
                }
                else
                {
                    if (word == "(")
                    {
                        level++;
                    }
                    else if (word == ")")
                    {
                        if (inExternalBlock && level == startLevel)
                        {

                            if(wasMysql)
                            {
                                string args = "";
                                builder.Append(ParseSQLAndArgs(InSQLBuilder.ToString(), out args));                                

                                builder.Append("\"");

                                if (!string.IsNullOrWhiteSpace(args))
                                {
                                    builder.Append(", " + args);
                                }
                                HasUsedMysql = true;
                                wasMysql = false;                                
                            }
                            else if (wasXML)
                            {
                                builder.Append(InSQLBuilder.ToString().Replace("\"", "\"\""));
                                builder.Append("\"");
                                wasXML = false;
                                HasUsedXML = true;
                            }
                            else
                            {
                                builder.Append(InSQLBuilder.ToString().Replace("\"", "\"\""));
                                builder.Append("\"");

                                HasUsedJson = true;
                            }
                            InSQLBuilder = new StringBuilder();
                            inExternalBlock = false;                            
                        }

                        level--;
                    }

                    if (!inExternalBlock && prevWord == "(" &&
                    ((lword == "select") ||
                    (lword == "delete") ||
                    (lword == "update") ||
                    (lword == "insert") ||
                    (lword == "{") ||
                     (lword == "<")))
                    {
                        builder.Length -= 2;
                        if (lword[0] == 's')
                        {
                            builder.Append("db.ReadData(@\"" + word);
                        }
                        else if (lword[0] == 'd' || lword[0] == 'u')
                        {
                            builder.Append("db.SetDataReturnNone(@\"" + word);
                        }
                        else if (lword[0] == '{')
                        {
                            builder.Append("OnlySQL.JSON.Parse(@\"" + word);                            
                        }
                        else if (lword[0] == '<')
                        {
                            builder.Append("OnlySQL.XML.Parse(@\"" + word);
                        }
                        else
                        {                            
                            builder.Append("db.SetDataReturnLastInsertId(@\"" + word);
                        }
                        wasXML = lword[0] == '<';
                        wasMysql = lword[0] != '{' && !wasXML;
                        

                        startLevel = level;
                        inExternalBlock = true;

                        prevWord = word;
                        continue;
                    }

                    if (inExternalBlock)
                    {
                        InSQLBuilder.Append(word);
                    }
                    else
                    {

                        if (lword == "commit")
                        {
                            //TransactionCommit
                            builder.Append("db.TransactionCommit()");
                        }
                        else if (lword == "revoke")
                        {
                            //TransactionCommit
                            builder.Append("db.TransactionRollback()");
                        }
                        else if (lword == "begin")
                        {
                            //TransactionCommit
                            builder.Append("db.BeginTransaction();");
                        }
                        else
                        {
                            builder.Append(word);
                        }
                    }

                }
             
                prevWord = word;
            }
            
            if(_trackTime)
            {
                sw.Stop();
                Console.WriteLine("Finished: Parsing Source " + sw.ElapsedMilliseconds + "ms");
                sw = Stopwatch.StartNew();
            }

            return builder.ToString();
        }

        private IEnumerable<string> ParseWords(string source)
        {
            int length = source.Length;
            var builder = new StringBuilder();
            var strBuilder = new StringBuilder();

            bool inDoubleQuote = false;
            bool inSingleQuote = false;


            for (int i = 0; i < length; i++)
            {
                if(inDoubleQuote)
                {
                    builder.Append(source[i]);

                    if (source[i] == '\"')
                    {
                        yield return builder.ToString();

                        builder = new StringBuilder();

                        inDoubleQuote = false;
                    }

                }
                else if(inSingleQuote)
                {
                    builder.Append(source[i]);

                    if (source[i] == '\'')
                    {                        
                        yield return builder.ToString();

                        builder = new StringBuilder();                        

                        inSingleQuote = false;                        
                    }
                }
                else
                {
                    if(source[i] == '\"')
                    {
                        if (builder.Length > 0)
                        {
                            yield return builder.ToString();

                            builder = new StringBuilder();
                        }

                        inDoubleQuote = true;
                        builder.Append(source[i]);
                    }else if (source[i] == '\'')
                    {
                        if (builder.Length > 0)
                        {
                            yield return builder.ToString();

                            builder = new StringBuilder();
                        }

                        inSingleQuote = true;
                        builder.Append(source[i]);
                    }
                    else
                    {
                        if (source[i] == ';' || source[i] == '(' || source[i] == ')' || source[i] == '+' || source[i] == '-' || source[i] == '{' || source[i] == '}' || source[i] == '/' || source[i] == '*' || source[i] == '>' ||  source[i] == '<')
                        {                            
                            if (builder.Length > 0)
                            {
                                yield return builder.ToString();

                                builder = new StringBuilder();
                            }

                            yield return source[i].ToString();
                        }
                        else
                        {
                            if (char.IsWhiteSpace(source[i]))
                            {
                                if (builder.Length > 0)
                                {
                                    yield return builder.ToString();

                                    builder = new StringBuilder();
                                }

                                yield return source[i].ToString();
                            }
                            else
                            {
                                builder.Append(source[i]);
                            }
                        }
                    }
                }                
            }
            if (builder.Length > 0)
            {
                var x = builder.ToString(); 

                if(x.StartsWith("'") || x.StartsWith("\""))
                {
                    yield return x[0].ToString();
                    foreach (var item in ParseWords(x.Substring(1)))
                    {
                        yield return item;
                    }
                }
                else
                {
                    yield return x;
                }                
            }
        }
        Stopwatch sw;
        bool _trackTime;

        public static void Setup()
        {
            CSScript.Evaluator.ReferenceAssemblyByNamespace("MySql.Data.MySqlClient");
            CSScript.Evaluator.ReferenceAssemblyByNamespace("System.Linq");
            CSScript.Evaluator.ReferenceAssemblyByNamespace("System.Collections.Generic");
            CSScript.Evaluator.ReferenceAssemblyByNamespace("System.IO.FileSystem");
            CSScript.Evaluator.ReferenceAssemblyByNamespace("Newtonsoft.Json");
            CSScript.Evaluator.ReferenceAssemblyByNamespace("System.Xml");
        }

        public void Run(string source, bool trackTime = false)
        {            
            //var x = new List<dynamic>();
            _trackTime = trackTime;
            if (_trackTime)
            {
                sw = Stopwatch.StartNew();
            }
            bool addUsingDb = false;
            bool addUsingJson = false;
            bool addUsingXML = false;
            source = Parse(source, out addUsingDb, out addUsingJson, out addUsingXML);

            if(addUsingDb)
            {
                source = 
@"using(OnlySQL.Database db = new OnlySQL.Database())
{
" + source + @"
}";
            }

            source = 
@"using System;" + 

(addUsingDb ? @"
using MySql;
using MySql.Data;
using MySql.Data.MySqlClient;" : "") +

(addUsingJson ? @"
using Newtonsoft;
using Newtonsoft.Json;" : "") + 

(addUsingXML ? @"
using System.Xml;" : "") + 

@"
using System.Linq;
using System.Collections;
using System.Collections.Generic;

void main()
{
" + source +  @"
}";
            if(trackTime)
                Console.WriteLine("CSharp: [" + source + "]");

            try
            {

                var main = CSScript.Evaluator
                                      .CreateDelegate(source);
                main();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }


            if (_trackTime)
            {
                sw.Stop();
                Console.WriteLine("Finished: Running " + sw.ElapsedMilliseconds + "ms");                
            }
        }
    }
}
