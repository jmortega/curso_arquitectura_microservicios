namespace Calculadora.Core.Interfaces;

/// <summary>Contrato con todas las operaciones de la calculadora.</summary>
public interface ICalculadora
{
    double Sumar(double a, double b);
    double Restar(double a, double b);
    double Multiplicar(double a, double b);
    double Dividir(double a, double b);
    double Potencia(double baseNum, double exponente);
    double RaizCuadrada(double numero);
    double Modulo(double a, double b);
    double Porcentaje(double valor, double porcentaje);
}
