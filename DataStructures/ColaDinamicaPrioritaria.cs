using SistemaBancario.Models;

namespace SistemaBancario.DataStructures
{
    public class ColaDinamicaPrioritaria
    {
        private Nodo? frente;
        private int contador;

        public ColaDinamicaPrioritaria()
        {
            frente = null;
            contador = 0;
        }

        public bool EstaVacia() => frente == null;
        public int ObtenerConteo() => contador;

        /// <summary>
        /// Inserta ordenadamente en la Cola 3 (Emisión SWIFT):
        /// 1° Mayor Nivel de Prioridad (ej. 5 > 3 > 1)
        /// 2° Hora de Emisión / Inserción Cronológica (FIFO para prioridades iguales)
        /// </summary>
        public void EncolarConPrioridad(Transaction transaccion, int prioridad)
        {
            Nodo nuevoNodo = new Nodo(transaccion, prioridad);

            // CASO 1: Cola vacía o el nuevo nodo tiene MAYOR prioridad que el frente actual
            if (EstaVacia() || prioridad > frente!.Prioridad)
            {
                nuevoNodo.Siguiente = frente;
                frente = nuevoNodo;
            }
            else
            {
                Nodo actual = frente;

                // Avanzar mientras el siguiente nodo tenga MAYOR prioridad que el nuevo
                // O si tienen IGUAL prioridad, mientras el siguiente nodo haya ingresado ANTES
                while (actual.Siguiente != null &&
                      (actual.Siguiente.Prioridad > prioridad ||
                      (actual.Siguiente.Prioridad == prioridad && actual.Siguiente.Transaccion.ExecutionTimestamp <= transaccion.ExecutionTimestamp)))
                {
                    actual = actual.Siguiente;
                }

                // Inserción en el punto dinámico correspondiente
                nuevoNodo.Siguiente = actual.Siguiente;
                actual.Siguiente = nuevoNodo;
            }

            contador++;
        }

        public Transaction? Desencolar()
        {
            if (EstaVacia()) return null;

            Transaction transaccion = frente!.Transaccion;
            frente = frente.Siguiente;
            contador--;
            return transaccion;
        }

        public List<Transaction> ObtenerElementosComoLista()
        {
            List<Transaction> lista = new List<Transaction>();
            Nodo? actual = frente;

            while (actual != null)
            {
                lista.Add(actual.Transaccion);
                actual = actual.Siguiente;
            }

            return lista;
        }
    }
}