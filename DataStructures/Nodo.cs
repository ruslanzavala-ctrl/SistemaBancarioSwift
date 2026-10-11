using SistemaBancario.Models;

namespace SistemaBancario.DataStructures
{
    public class Nodo
    {
        public Transaction Transaccion { get; set; }
        public int Prioridad { get; set; } // <--- Añadido para establecer la prioridad que el nodo debe seguir
        public Nodo? Siguiente { get; set; }

        // Constructor que acepta la transacción y su nivel de prioridad
        public Nodo(Transaction transaccion, int prioridad)
        {
            Transaccion = transaccion;
            Prioridad = prioridad;  // <--- Inicializa la prioridad del nodo según la transacción
            Siguiente = null;
        }
    }
}