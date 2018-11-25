package isel.leic.pc.utils.optbuild;
/*
 * Infra-estrutura para a leitura de argumentos de invocação de um programa.
 *
 * Assume-se o seguinte formato para os argumentos
 *
 * prog <options_and_args>
 *
 * <options_and_args> ::=  {{'/' | '-' }<option> [ ':' <value> ] }*  { <arg> }*
 *
 * Jorge Martins, 2017
 */

import java.lang.reflect.Field;
import java.util.LinkedList;
import java.util.List;

import java.lang.reflect.*;
import java.util.LinkedList;
import java.util.List;

/**
 * descritor de opção de invocação de programa
 */


/// <summary>
/// Summary description for Class1.
/// </summary>
public class Options
{


    private List<String> args;
    private List<OptionDescriptor> optionsList;


    // métodos auxiliares
    private static String optionName(String option)
    {
        String name = option.substring(1);
        int dotIndex = name.indexOf(':');
        if (dotIndex != -1) name = name.substring(0, dotIndex);
        return name;
    }

    private List<OptionDescriptor> buildOptionList()
    {
        Class t = getClass();
        Field[] fields = t.getFields();
        for  (Field  f : fields)
        {
            Object[] options = f.getAnnotationsByType(Option.class);
            Option  attr = null;
            if (options.length == 1)
            {
                attr = (Option)options[0];
                optionsList.add(new OptionDescriptor(f, attr));
            }

        }
        return optionsList;
    }

    private OptionDescriptor getOptionByName(String optionName)
    {
        for(OptionDescriptor option : optionsList)
        {
            String nickname = option.getNickName();
            String name = option.getName();
            if (option.getNickName().equals(optionName) || option.getName().equals(optionName))
                return option;
        }
        return null;
    }

    private void processOption(String arg) throws Exception
    {
        // obter descritor da opção e sinalizar erro, caso a opção seja desconhecida
        OptionDescriptor option = getOptionByName(optionName(arg));

        if (option == null)
            throw new Exception("Invalid Option");

        option.setValue(this, arg);

    }

    private void processArg(String arg)
    {
        args.add(arg);
    }


    private boolean isOption(String arg)
    {
        return arg.charAt(0)  == '/' || arg.charAt(0) == '-';
    }

    /// <summary>
    /// contrutor
    /// </summary>
    public Options() {
        args = new LinkedList<String>();
        optionsList = new LinkedList<OptionDescriptor>();
        buildOptionList();

    }

    /// <summary>
    /// apresenta a string que mostra a forma de invoca��o do programa que usa estas op��es
    /// </summary>
    public void usage() {
        String name = "program ";

        System.out.printf("usage: %s", name);

        for( OptionDescriptor option : optionsList) {
            System.out.print(option);
            System.out.print(' ');
        }
        System.out.println();
    }

    /// <summary>
    /// mensagem de help usada quando o programa se invoca com a opção -?
    /// </summary>
    public void help() {
        usage();
        System.out.println();
        for (OptionDescriptor option : optionsList)
        {
            option.showHelp();


        }
        System.out.println();
    }

    public Object[] Arguments()
    {
        return args.toArray();
    }

    /// <summary>
    /// parsing da linha de comando
    /// </summary>
    /// <param name="args"></param>
    public void load(String[] args)  throws Exception
    {
        int i=0;
        while  ( i < args.length && isOption(args[i]))
        {
            if (args[i].charAt(1)  == '?')
            {
                help();
                System.exit(0);
            }
            processOption(args[i]);
            ++i;
        }
        while (i < args.length && !isOption(args[i]))
        {
            processArg(args[i]);
            ++i;
        }
    }

}