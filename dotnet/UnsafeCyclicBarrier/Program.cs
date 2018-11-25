using System;
using System.Threading;

namespace UnsafeCyclicBarrier {
    
    /// <summary>
    /// Esta implementação reflete a semântica de sincronização de uma barreira cíclica
    /// (e.g., uma barreira que pode ser usada repetidamente para sincronizar o mesmo 
    /// grupo de threads) , contudo não é thread-safe.Implemente em Java ou C#, sem utilizar locks, 
    /// uma versão thread-safe deste sincronizador.
    /// </summary> 
    public class UnsafeCyclicBarrier {
        private readonly int partners;
        private int remaining, currentPhase;
        public UnsafeCyclicBarrier(int partners) {
            if (partners <= 0) throw new ArgumentException();
            this.partners = this.remaining = partners;
        }
        public void signalAndAwait() {
            int phase = currentPhase;
            if (remaining == 0) throw new ArgumentException();
            if (--remaining == 0) {
                remaining = partners; currentPhase++;
            }
            else {
                while (phase == currentPhase) Thread.Yield();
            }
        }
    }

    /// <summary>
    /// Esta é uma tentativa de realização de uma alternativa thread-safe da barreira cíclica
    /// mas não é suficientemente robusta no cenário de máutilização em que mais do que "partners"
    /// threads invoquem a operação "signalAndAwait". 
    /// Na linha 60, a thread que termina a ronda tem de afectar o estado da próxima ronda. 
    /// Ora como essa alteração não é atómica pode acontecer que após a reiniciação de "remaining" 
    /// outra thread invoque "signalAndAwait" decremente remaining e fique à espera da mudança 
    /// de fase. Se a thread que terminou a ronda só agora fizer a alteração de fase, 
    /// a thread "intrusa" é acordada e avança como se a ronda tivesse terminado, o que claramente 
    /// não aconteceu (no pressuposto de que "remaining" é maior que zero).
    /// </summary>
    public class SafeCyclicBarrier {
        private readonly int partners;
        private volatile int remaining, currentPhase;

        public SafeCyclicBarrier(int partners) {
            if (partners <= 0) throw new ArgumentException();
            this.partners = remaining = partners;
        }

        public void signalAndAwait() {
            int phase;
            while (true) {
                phase = currentPhase;
                int obsRemaining = remaining;
                if (obsRemaining == 0) throw new ArgumentException();
                if (Interlocked.CompareExchange(ref remaining, obsRemaining - 1, obsRemaining)
                    == obsRemaining) {
                    if (obsRemaining == 1) {
                        remaining = partners;
                        currentPhase++;
                        return;
                    }
                    else {
                        break;
                    }
                }
            }
            while (phase == currentPhase) Thread.Yield();
        }
    }

    /// <summary>
    /// Esta implementação tira partido de um objecto (instância de PhaseDescriptor)
    /// que encapsula o estado da ronda (o objecto em si representa a fase).
    /// A thread responsável por iniciar nova ronda cria um novo objecto devidamente
    /// iniciado. O objecto que representa a fase anterior mantém o valor de "remaining" a zero,
    /// pelo que qualquer thread "intrusa" que invoque "signalAndAwait" antes da alteração da fase
    /// vê a operação falhar (por lançamento da excepção  InvalidOperationException).
    /// Deste modo o invariante da barreira ciclica (nova ronda ao fim de "partners" chamadas
    /// a "signalAndWait") mantém-se correto em qualquer circunstância.
    /// </summary>
    public class SafeCyclicBarrierOk {
        private readonly int partners;

        private class PhaseDescriptor {
            internal int remaining;
            public PhaseDescriptor(int remaining) {
                this.remaining = remaining;
            }
        }

        private volatile PhaseDescriptor currentPhase;

        public SafeCyclicBarrierOk(int partners) {
            if (partners <= 0) throw new ArgumentException();
            this.partners = partners;
            currentPhase = new PhaseDescriptor(partners);
        }

        public void signalAndAwait() {
            PhaseDescriptor observedPhase;
            while (true) {
                observedPhase = currentPhase;

                int remaining = observedPhase.remaining;
                if (remaining <= 0) throw new InvalidOperationException();
                if (Interlocked.CompareExchange(ref currentPhase.remaining,
                        remaining - 1, remaining) == remaining) {
                    if (remaining == 1) {
                        currentPhase = new PhaseDescriptor(partners);
                        return;
                    }
                    else {
                        break;
                    }
                }
            }
            while (observedPhase == currentPhase) Thread.Yield();
        }
    }
    class Program {
        static void Main(string[] args) {
        }
    }
}
