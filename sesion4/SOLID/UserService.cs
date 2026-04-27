using System;

//Acoplamiento fuerte: La lógica de verificar si hay dinero en el banco
//(HasMoneyAtBank) está escrita dentro de la misma clase UserService.

//Violación de responsabilidades: Un servicio de usuario no debería gestionar lógicas bancarias internas
//; solo debería coordinar acciones del usuario.

//Dificultad para testear: Si HasMoneyAtBank fuera una llamada real a una base de datos o API,
//no podrías probar PayMyDebts de forma aislada sin tocar el banco real.

class UserService
{
    public bool PayMyDebts(string userID)
    {
        var success = false;

        // El usuario necesita saber si tiene dinero en el banco para cubrir su deuda del mes
        if (HasMoneyAtBank(userID))
        {
            // El usuario va a pagar sus deudas para el mes actual
            success = true;
        }

        return success;
    }

    public bool HasMoneyAtBank(string userId)
    {
        return true;
    }
}

