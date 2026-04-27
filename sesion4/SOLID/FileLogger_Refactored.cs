using System;

//Aquí se intenta dar soporte a múltiples destinos (Archivo y Base de Datos), pero de manera incorrecta.

//Patrón aplicado: Se intenta separar la lógica de destino creando DbLogger y FileLogger.

//El nuevo problema: ExceptionLogger ahora tiene múltiples métodos (LogIntoFile, LogIntoDataBase).
//Esto hace que la clase crezca cada vez que queramos añadir un nuevo destino (como un log de eventos o la nube).

//Violación de SRP y OCP: ExceptionLogger ahora "sabe demasiado" sobre cómo instanciar cada tipo de logger.

public class DbLogger
{
    public void LogMessage(string aMessage)
    {
        //Code to write message in database.   
    }
}
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
    public void LogIntoDataBase(Exception aException)
    {
        DbLogger objDbLogger = new DbLogger();
        objDbLogger.LogMessage(GetUserReadableMessage(aException));
    }
    private string GetUserReadableMessage(Exception ex)
    {
        string strMessage = string.Empty;
        //code to convert Exception's stack trace and message to user readable format.   
        ////////

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
        catch (IOException ex)
        {
            new ExceptionLogger().LogIntoDataBase(ex);
        }
        catch (Exception ex)
        {
            new ExceptionLogger().LogIntoFile(ex);
        }
    }
}
