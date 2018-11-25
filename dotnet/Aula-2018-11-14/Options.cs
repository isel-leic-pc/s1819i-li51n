/*
 * Infra-estrutura para a leitura de argumentos de invocação de um programa.
 * 
 * Assume-se o seguinte formato para os argumentos
 * 
 * prog <options_and_args>
 * 
 * <options_and_args> ::=  {{'/' | '-' }<option> [ ':' <value> ] }*  { <arg> }*
 * 
 * O projecto testoptbuild testa este assembly
 */


using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;


namespace ReflectionUtils
{
    /// <summary>
    /// descritor de opção de invocação de programa
    /// </summary>
     class OptionDescriptor {   
        OptionAttribute attr;
        FieldInfo fi;

        private static  string optionValue(string option) {
            int dotIndex = option.IndexOf(':');
            if (dotIndex != -1) return option.Substring(dotIndex + 1);
            return null;
		}

		internal OptionDescriptor(FieldInfo fi, OptionAttribute attr) { 
            this.fi= fi; 
            this.attr = attr;
        }
      
		internal string Name 
		{
            get
            {
               return fi.Name;
            }
		}

        internal string NickName {
            get {
                if (attr == null || attr.Nickname == null) return Name;
                return attr.Nickname;
            }
        }

        /// <summary>
        /// Afecta o campo correspondente a este descritor de opção no objecto e com o valor passados por argumento
        /// </summary>
        /// <param name="o"></param>
        /// <param name="arg"></param>
        internal void setValue(object o, string arg) {
            Type t = fi.FieldType;
			if (t == typeof(bool)) 
				fi.SetValue(o, true);
			else {
                string val = optionValue(arg);
                fi.SetValue(o, Convert.ChangeType(val, fi.FieldType));
            }
        }

         
        /// <summary>
        /// retorna a string que representa o descritor de opção no formato adequado ao método usage
        /// </summary>
        /// <returns></returns>
        public override string  ToString() {
 	        StringBuilder sb = new StringBuilder();
            sb.Append('/');
            sb.Append(NickName);
            if (Name != NickName) {
                sb.Append('(');
                sb.Append(Name);
                sb.Append(')');
            }
            Type t = fi.FieldType;
            if (t != typeof(bool)) {
                sb.Append(":<");
                sb.Append(t.Name);
                sb.Append(" value>");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Mostra a linha de help associada à opção
        /// </summary>
        internal void showHelp()
        {
            Console.Write("{0} {1}", fi.FieldType.Name, fi.Name);

            if (attr != null)
            {
                Console.WriteLine("({0}):\t{1}", attr.Nickname, attr.Description);
            }
            else Console.WriteLine();
        }
		

	}


	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	public class Options
	{
		

        private List<string> args;
        List<OptionDescriptor> optionsList;


        // métodos auxiliares

        private static string optionName(string option)
        {
            string name = option.Substring(1);
            int dotIndex = name.IndexOf(':');
            if (dotIndex != -1) name = name.Substring(0, dotIndex);
            return name;
        }

        private List<OptionDescriptor> buildOptionList()
        {
            Type t = GetType();
            FieldInfo[] fields = t.GetFields();
            foreach (FieldInfo f in fields)
            {
                object[] options = f.GetCustomAttributes(typeof(OptionAttribute), false);
                OptionAttribute attr = null;
                if (options.Length == 1)
                {
                    attr = (OptionAttribute)options[0];
                }
                optionsList.Add(new OptionDescriptor(f, attr));
            }
            return optionsList;
        }

        private OptionDescriptor getOptionByName(string optionName)
        {
            foreach (OptionDescriptor option in optionsList)
            {
                if (option.NickName == optionName || option.Name == optionName) return option;
            }
            return null;
        }

        private void processOption(string arg)
        {

            // obter descritor da opção e sinalizar erro, caso a opção seja desconhecida
            OptionDescriptor option = getOptionByName(optionName(arg));

            if (option == null)
                throw new Exception("Invalid Option");

            option.setValue(this, arg);

        }

        private void processArg(string arg)
        {
            args.Add(arg);
        }

     
        private bool isOption(string arg)
        {
            return arg[0] == '/' || arg[0] == '-';
        }

        /// <summary>
        /// contrutor
        /// </summary>
        public Options() {
            args = new List<string>();
            optionsList = new List<OptionDescriptor>();
            buildOptionList();
        
        }

	    /// <summary>
	    /// apresenta a string que mostra a forma de invocação do programa que usa estas opções
	    /// </summary>
        public void usage() {
            string name = Assembly.GetEntryAssembly().FullName;
           
            Console.Write("usage: {0} ", name.Substring(0,name.IndexOf(',')));
            foreach( OptionDescriptor option in optionsList) {
                Console.Write(option);
                Console.Write(' ');
            }
            Console.WriteLine();
        }

        /// <summary>
        /// mensagem de help usada quando o programa se invoca com a opção -?
        /// </summary>
        public void help() {
            usage();
            Console.WriteLine();
            foreach (OptionDescriptor option in optionsList)
            {
                option.showHelp();
              
          
            }
            Console.WriteLine();
        }

        public string[] Arguments
        {
            get { return args.ToArray(); }
        }

        /// <summary>
        /// parsing da linha de comando
        /// </summary>
        /// <param name="args"></param>
		public void load(string[] args) 
		{
            int i=0;
			while  ( i < args.Length && isOption(args[i]))  
			{
                if (args[i][1] == '?')
                {
                    help();
                    Environment.Exit(0);
                }
			    processOption(args[i]);
                ++i;
			}
            while (i < args.Length && !isOption(args[i]))
            {
                processArg(args[i]);
                ++i;
            }
		}
			
	}
}
