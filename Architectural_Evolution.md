# Introduction #

This is a draft coverage of the architectural evolution that Seacow has undergone. Given that this was added at this late stage, details are missing, and the points listed will undergo regular revisions.


# Details #
## Version 1? ##
  * Dictionary collections were used, primarily for the TryGetValue function to trigger File reads.

  * Core.DataStructure consisted of Database.cs, Table.cs, Row.cs. Fields were part of an Object array within the Row.

  * Columns were established as fixed length, so as to provide some manner of indexing via simple arithmetic.

  * The system was built upon a ReadCommitted isolation level, with ReadWriteLock acquisition and release dealt with internally within the CRUD functions of the Table.cs.

  * Issues surrounded the ReadWriteLock, particularly starvation of writers and readers in high traffic load situations.

  * The Field object was then introduced to the system, and CRUD operations were based on collections of Fields to indicate original values and new values.

  * StringField.cs and Int32Field.cs were introduced to allow for alphanumeric and numeric data values respectively. Field data was stored internally in memory as native types.

  * A Client and Server class and harness was produced to handle traffic into and out of the DB.

  * File metadata was introduced to track rows.

  * The Core.CommandPattern was introduced to translate user commands into DB operations.

  * SQL syntax was to be supported by the Core.CommandPattern.

  * Low level synchronization (lock keyword) was overlooked due to assumed dependence on the ReadWriteLock to maintain data integrity.

  * Column.cs was introduced to provide a more elegant way of referencing between the Table, and Fields for a given Row.

  * Field data has been changed to store Byte arrays internally , using abstract translation methods to access and mutate the values.

  * The new Seacow query language and syntax was introduced to simply command parsing.

  * The Client component was discarded in favor of TelNet connectivity. It faces issues however, with typographical errors and certain keys (up, down, backspace, etc).

  * Transaction support was added, via the use of anonymous delegates, and the escalation of ReadWriteLock Acquisition and Release followed, to be handled by the given transaction.

  * Different isolation level support was introduced with the addition of DbEnvironment.cs, with support for ReadUncommited, RepeatableRead and Serializable.

  * Change Of the networking architecture was introduced. Where initially it was hooked up to localhost, changed to the public IP address of the computer.

  * To accomodate the TelNet connectivity, modifications were made to the connection component : BinaryReader & BinaryWriter into StreamReader & StreamWriter.