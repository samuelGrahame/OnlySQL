using CSScriptLibrary;
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

        private string Parse(string source)
        {
            if (_trackTime)
            {                
                Console.WriteLine("Started: Parsing Source");
            }

            source = CleanWhiteSpace(source);
            var builder = new StringBuilder();
            var InSQLBuilder = new StringBuilder();
            bool inSQL = false;
            int startLevel = 0;
            int level = 0;
            string prevWord = "";

            foreach (var word in ParseWords(source))
            {
                if(word == "(")
                {                    
                    level++;
                }
                else if (word == ")")
                {
                    if(inSQL && level == startLevel)
                    {
                        string args = "";
                        builder.Append(ParseSQLAndArgs(InSQLBuilder.ToString(), out args));
                        InSQLBuilder = new StringBuilder();
                        
                        builder.Append("\"");

                        if(!string.IsNullOrWhiteSpace(args))
                        {
                            builder.Append(", " + args);
                        }

                        inSQL = false;
                    }

                    level--;                    
                }
                string lword = word.ToLower();

                if (!inSQL && prevWord == "(" && 
                    (lword == "select") ||
                    (lword == "delete") ||
                    (lword == "update") ||
                    (lword == "insert"))
                {
                    builder.Length-=2;
                    if (lword[0] == 's')
                    {
                        builder.Append("db.ReadData(@\"" + word + " ");
                    }else if (lword[0] == 'd' || lword[0] == 'u')
                    {
                        builder.Append("db.SetDataReturnNone(@\"" + word + " ");
                    }
                    else
                    {
                        builder.Append("db.SetDataReturnLastInsertId(@\"" + word + " ");
                    }

                    startLevel = level;
                    inSQL = true;


                }
                else
                {
                    if(inSQL)
                    {
                        if (word == "(" && InSQLBuilder.Length > 0)
                            InSQLBuilder.Length--;
                        InSQLBuilder.Append(word + " ");
                    }
                    else
                    {
                        
                        if (lword == "commit")
                        {
                            //TransactionCommit
                            builder.Append("db.TransactionCommit()");
                        }else if (lword == "revoke")
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
                            builder.Append(word + " ");
                        }                        
                    }
                    
                }                
                prevWord = word;
            }
            if (builder.Length > 0)
                builder.Length--;

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

            for (int i = 0; i < length; i++)
            {
                if (source[i] == ';' || source[i] == '(' || source[i] == ')' || source[i] == '+' || source[i] == '-' || source[i] == '{' || source[i] == '}' || source[i] == '/' || source[i] == '*')
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
                    }
                    else
                    {
                        builder.Append(source[i]);
                    }
                }
                
            }
            if (builder.Length > 0)
            {
                yield return builder.ToString();
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
        }

        public void Run(string source, bool trackTime = false)
        {            
            //var x = new List<dynamic>();
            _trackTime = trackTime;
            if (_trackTime)
            {
                sw = Stopwatch.StartNew();
            }
            
            var main = CSScript.Evaluator
                                  .CreateDelegate(
@"
using System;
using MySql;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

void main()
{
using(OnlySQL.Database db = new OnlySQL.Database())
{
" + Parse(source) + @"
}
}");
            main();

            if (_trackTime)
            {
                sw.Stop();
                Console.WriteLine("Finished: Running " + sw.ElapsedMilliseconds + "ms");                
            }
        }
    }
}
