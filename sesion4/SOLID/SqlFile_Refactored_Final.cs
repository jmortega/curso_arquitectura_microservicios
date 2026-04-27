using System;

//En la versión final, se aplica la Segregación de Interfaces (ISP) y
//se corrige la jerarquía de herencia para que sea semánticamente correcta.

//A. Segregación de Interfaces
//Se divide la responsabilidad en dos capacidades atómicas:
//IReadableSqlFile: Contrato para objetos que solo pueden leerse (LoadText).
//IWritableSqlFile: Contrato para objetos que pueden escribirse (SaveText).


//B. Implementación Especializada
//ReadOnlySqlFile: Implementa solo IReadableSqlFile. Es imposible llamar a SaveText por
//error porque el método ni siquiera existe para esta clase.
//SqlFile: Implementa ambas interfaces, ya que es un archivo completo.

//C. Seguridad en el Manager
//La clase SqlFileManager ahora es totalmente segura y predecible:
//El método GetTextFromFiles recibe una lista de IReadableSqlFile.
//No le importa si son archivos normales o de solo lectura, porque ambos garantizan la lectura.
//El método SaveTextIntoFiles recibe una lista de IWritableSqlFile.
//El compilador garantiza que todos los objetos en esa lista tienen un método SaveText funcional.
//Ya no hace falta usar if ni capturar excepciones de "no implementado".

public interface IReadableSqlFile
{
    string LoadText();
}
public interface IWritableSqlFile
{
    void SaveText();
}

public class ReadOnlySqlFile : IReadableSqlFile
{
    public string FilePath { get; set; }
    public string FileText { get; set; }
    public string LoadText()
    {
        /* Code to read text from sql file */
    }
}

public class SqlFile : IWritableSqlFile, IReadableSqlFile
{
    public string FilePath { get; set; }
    public string FileText { get; set; }
    public string LoadText()
    {
        /* Code to read text from sql file */
    }
    public void SaveText()
    {
        /* Code to save text into sql file */
    }
}

public class SqlFileManager
{
    public string GetTextFromFiles(List<IReadableSqlFile> aLstReadableFiles)
    {
        StringBuilder objStrBuilder = new StringBuilder();
        foreach (var objFile in aLstReadableFiles)
        {
            objStrBuilder.Append(objFile.LoadText());
        }
        return objStrBuilder.ToString();
    }
    public void SaveTextIntoFiles(List<IWritableSqlFile> aLstWritableFiles)
    {
        foreach (var objFile in aLstWritableFiles)
        {
            objFile.SaveText();
        }
    }
}

