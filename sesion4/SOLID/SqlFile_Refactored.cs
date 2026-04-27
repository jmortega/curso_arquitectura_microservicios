using System;

//Aquí se intenta introducir la funcionalidad de "solo lectura" mediante herencia,
//lo cual genera una violación de los principios SOLID.

//El error de diseño: Se crea ReadOnlySqlFile heredando de SqlFile.
//Sin embargo, como un archivo de solo lectura no puede guardar, el programador lanza una excepción
//dentro de SaveText().

//Violación de LSP (Principio de Sustitución de Liskov): Este principio dice que una clase hija
//debe poder sustituir a su clase padre sin que el programa falle.
//si SqlFileManager intenta guardar una lista de archivos y uno de ellos es ReadOnlySqlFile,
//el programa "explota" con una excepción.

//Solución "parche": Para evitar el error, el programador añade un if (!objFile is ReadOnlySqlFile)
//en el manager. Esto es un code smell, ya que el manager no debería tener
//que preguntar el tipo de clase para saber si puede usar sus métodos.

public class SqlFile
{
    public string LoadText()
    {
        /* Code to read text from sql file */
    }
    public void SaveText()
    {
        /* Code to save text into sql file */
    }
}
public class ReadOnlySqlFile : SqlFile
{
    public string FilePath { get; set; }
    public string FileText { get; set; }
    public string LoadText()
    {
        /* Code to read text from sql file */
    }
    public void SaveText()
    {
        /* Throw an exception when app flow tries to do save. */
        throw new IOException("Can't Save");
    }
}
public class SqlFileManager
{
    public List<SqlFile> lstSqlFiles {get; set; }
    public string GetTextFromFiles()
    {
        StringBuilder objStrBuilder = new StringBuilder();
        foreach (var objFile in lstSqlFiles)
        {
            objStrBuilder.Append(objFile.LoadText());
        }
        return objStrBuilder.ToString();
    }
    public void SaveTextIntoFiles()
    {
        foreach (var objFile in lstSqlFiles)
        {
            //Check whether the current file object is read only or not.If yes, skip calling it's   
            // SaveText() method to skip the exception.   

            if (!objFile is ReadOnlySqlFile)
                objFile.SaveText();
        }
    }   
}   

