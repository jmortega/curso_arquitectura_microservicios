using System;

//En este punto tenemos una interfaz llamada ILead.

//Diseño: La interfaz asume que cualquier líder debe poder crear subtareas, asignar tareas y
//trabajar en ellas (WorkOnTask).

//Uso: El TeamLead implementa esto sin problemas porque en muchas empresas un Team Lead también programa.

public interface ILead
{
    void CreateSubTask();
    void AssginTask();
    void WorkOnTask();
}
public class TeamLead : ILead
{
    public void AssignTask()
    {
        //Code to assign a task.   
    }
    public void CreateSubTask()
    {
        //Code to create a sub task   
    }
    public void WorkOnTask()
    {
        //Code to implement perform assigned task.   
    }
}

