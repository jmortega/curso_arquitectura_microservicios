using System;

//El problema surge cuando intentamos reutilizar la interfaz para un rol diferente, como un Manager.

//El Problema: Un Manager puede asignar tareas y crear subtareas, pero generalmente no desarrolla

//Violación de LSP (Principio de Sustitución de Liskov): Al implementar ILead,
//el Manager se ve obligado a definir el método WorkOnTask, pero como no puede cumplirlo,
//lanza una excepción: throw new Exception("Manager can't work on Task").

//Consecuencia: Esto rompe el programa si alguien intenta tratar a un Manager como un trabajador técnico,
//esperando que el método funcione.

public interface ILead
{
    void CreateSubTask();
    void AssginTask();
    void WorkOnTask();
}

public class Manager : ILead
{
    public void AssignTask()
    {
        //Code to assign a task.   
    }
    public void CreateSubTask()
    {
        //Code to create a sub task.   
    }
    public void WorkOnTask()
    {
        throw new Exception("Manager can't work on Task");
    }
}

