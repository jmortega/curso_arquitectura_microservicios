using System;

//Aplicar ISP (Principio de Segregación de Interfaces)

//A. Segregación de Interfaces (ISP)
//En lugar de una única interfaz con muchas responsabilidades, se divide en interfaces más pequeñas
//y específicas

//IProgrammer: Solo contiene WorkOnTask.

//ILead: Contiene AssignTask y CreateSubTask.

//B. Implementación por Composición de Roles
//Ahora, cada clase solo implementa lo que realmente sabe hacer:

//Programmer: Solo implementa IProgrammer.

//Manager: Solo implementa ILead. Ya no tiene métodos que lancen excepciones por falta de capacidad.

////TeamLead: Implementa ambas (IProgrammer, ILead).

/// Esto es polimorfismo: un Team Lead es tanto un programador como un líder

//Se atomizan las interfaces. El sistema ahora es más flexible: si mañana surge un nuevo rol
//(ej. un "Consultor" que solo programa pero no lidera), solo tiene que implementar IProgrammer sin cargar
//con la lógica de gestión de tareas

public interface IProgrammer
{
    void WorkOnTask();
}

public interface ILead
{
    void AssignTask();
    void CreateSubTask();
}

public class Programmer : IProgrammer
{
    public void WorkOnTask()
    {
        //code to implement to work on the Task.   
    }
}
public class Manager : ILead
{
    public void AssignTask()
    {
        //Code to assign a Task   
    }
    public void CreateSubTask()
    {
        //Code to create a sub taks from a task.   
    }
}

public class TeamLead : IProgrammer, ILead
{
    public void AssignTask()
    {
        //Code to assign a Task   
    }
    public void CreateSubTask()
    {
        //Code to create a sub task from a task.   
    }
    public void WorkOnTask()
    {
        //code to implement to work on the Task.   
    }
}
