# Introduction #

Simple listed evolution of the ReadWriteLock used within Seacow.


# Details #

  * Version 1:
    * ReadWriteLock in primitive stage.
    * Faced starvation issues with high traffic loads, where writers would not be able to obtain the lock.

  * Version 2:
    * ReadWriteLock introduced waitingReaders and waitingWriters, to allow for Readers obtain the lock in between two or more waiting Writers.
    * Problem raised was that without a queueing mechanism, queue cutting occurred.

  * Version 3:
    * ReadWriteLock introduced the use of the FIFOSemaphore so as to ensure the integrity of the First In First Out queueing of threads, categorised by their type (reader/writer).

  * Version 4:
    * ReadWriteLock introduced disposable locks that were issued out upon acquisition that were disposed of upon release so as to allow for Transaction support and ReadWriteLock management. Problem raised involved lock escalation, upgrades and downgrades from Writer to Reader, and vice versa.

  * Version 5:
    * ReadWriteLock introduced simple fields which tracked the current Thread holding the Writer Lock so as to allow for read operations to acquire a phantom Reader Lock.
    * Problem raised was the lack of interrupt handling capability.

  * Version 6:
    * ReadWriteLock refactoring was performed to introduce the Lightswitch utility to encapsulate Reader Lock Acquisition and Release. (Still in testing.)