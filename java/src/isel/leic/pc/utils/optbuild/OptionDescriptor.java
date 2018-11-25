package isel.leic.pc.utils.optbuild;


import java.lang.reflect.Field;
import java.lang.reflect.Type;

public class OptionDescriptor {
    Option  attr;
    Field fi;

    private static  String optionValue(String option) {
        int dotIndex = option.indexOf(':');
        if (dotIndex != -1) return option.substring(dotIndex + 1);
        return null;
    }

    OptionDescriptor(Field  fi, Option  attr) {
        this.fi= fi;
        this.attr = attr;
    }

    String getName()  { return fi.getName(); }

    String getNickName() {
        if (attr == null || attr.nickname() == null) return getName();
        return attr.nickname();
    }

    /**
     * Afecta o campo correspondente a este descritor de op��o no objecto e com o valor passados por argumento
     */
    void setValue(Object o, String arg) throws IllegalAccessException {
        Type t = fi.getType();
        if (t == boolean.class)
            fi.setBoolean(o, true);
        else {
            String val = optionValue(arg);
            if (t == int.class)
                fi.setInt(o, Integer.parseInt(val));
            else
                fi.set(o, val);
        }
    }


    /// <summary>
    /// retorna a string que representa o descritor de op��o no formato adequado ao m�todo usage
    /// </summary>
    /// <returns></returns>
    public String  toString() {
        StringBuilder sb = new StringBuilder();
        sb.append('/');
        sb.append(getNickName());
        if (!getName().equals(getNickName())) {
            sb.append('(');
            sb.append(getName());
            sb.append(')');
        }
        Class t = fi.getType();
        if (t != boolean.class ) {
            sb.append(":<");
            sb.append(t.getSimpleName());
            sb.append(" value>");
        }
        return sb.toString();
    }

    /// <summary>
    /// Mostra a linha de help associada � op��o
    /// </summary>
    void showHelp()
    {
        System.out.printf( "%s %s", fi.getType().getName(), fi.getName());

        if (attr != null)
        {
            System.out.printf( "(%s):\t %s", attr.nickname(), attr.description() );


        }
        else System.out.println();
    }


}
