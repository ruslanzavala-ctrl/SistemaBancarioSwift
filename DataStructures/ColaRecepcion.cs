using SistemaBancario.Models;

namespace SistemaBancario.DataStructures
{
    public class ColaRecepcion
    {
        private Nodo? frente;
        private Nodo? final;
        private int contador;

        public ColaRecepcion()
        {
            frente = null;
            final = null;
            contador = 0;
        }

        public bool EstaVacia() => frente == null;

        public int ObtenerConteo() => contador;

        // Encolar al final de la fila (Llegada cronológica)
        public void Encolar(Transaction transaccion)
        {
            Nodo nuevoNodo = new Nodo(transaccion, 0); // Prioridad por defecto
            if (EstaVacia())
            {
                frente = nuevoNodo;
                final = nuevoNodo;
            }
            else
            {
                if (final != null)
                {
                    final.Siguiente = nuevoNodo;
                    final = nuevoNodo;
                }
            }
            contador++;
        }

        // Desencolar el elemento del frente (FIFO)
        public Transaction? Desencolar()
        {
            if (EstaVacia()) return null;

            Transaction transaccion = frente!.Transaccion;
            frente = frente.Siguiente;
            contador--;

            if (frente == null)
            {
                final = null;
            }

            return transaccion;
        }

        public Transaction? VerFrente()
        {
            return frente?.Transaccion;
        }

        // Método auxiliar para exportar los elementos en formato de lista (sin alterar la cola)
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