using System;


//La clase ExceptionLogger tiene la responsabilidad de registrar excepciones,
// pero está obligada a usar exclusivamente FileLogger.

//Problema: Si quisiéramos registrar la excepción en una base de datos en lugar de un archivo,
//tendríamos que modificar el código interno de ExceptionLogger.

//Violación del principio Open/Closed:El código no está cerrado a la modificación ni abierto a la extensión.

public class FileLogger
{
    public void LogMessage(string aStackTrace)
    {
        //code to log stack trace into a file.   
    }
}
public class ExceptionLogger
{
    public void LogIntoFile(Exception aException)
    {
        FileLogger objFileLogger = new FileLogger();
        objFileLogger.LogMessage(GetUserReadableMessage(aException));
    }
    private GetUserReadableMessage(Exception ex)
    {
        string strMessage = string.Empty;
        //code to convert Exception's stack trace and message to user readable format.   
        ///////

        return strMessage;
    }
}

public class DataExporter
{
    public void ExportDataFromFile()
    {
        try
        {
            //code to export data from files to database.   
        }
        catch (Exception ex)
        {
            new ExceptionLogger().LogIntoFile(ex);
        }
    }
}


