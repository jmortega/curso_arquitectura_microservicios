using System;

//A. Principio de Responsabilidad Única (SRP)
//Se ha extraído la lógica bancaria a una nueva clase llamada BankService.
//Ahora, cada clase tiene una única razón para cambiar: UserService cambia si cambia el flujo del usuario,
//y BankService cambia si cambia la lógica de consulta bancaria.

//B. Inyección de Dependencias (Dependency Injection)
//En lugar de que UserService cree o sea dueño de la lógica bancaria,
//ahora la "recibe" a través de su constructor

//C. Delegación (Delegation)
//El método PayMyDebts ya no intenta resolver el problema por sí mismo.
//En su lugar, delega la pregunta al experto en la materia (_bankService):

class UserService
{
    private readonly BankService _bankService;

    public UserService(BankService bankService)
    {
        _bankService = bankService;
    }

    public bool PayMyDebts(string userID)
    {
        var success = false;

        // El usuario necesita saber si tiene dinero en el banco para cubrir su deuda del mes
        if (_bankService.HasMoneyAtBank(userID))
        {
            // El usuario va a pagar sus deudas para el mes actual
            success = true;
        }

        return success;
    }

}

class BankService
{
    public bool HasMoneyAtBank(string userId)
    {
        return true;
    }
}

