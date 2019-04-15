# Structured Logfile Format

This implementation for the [Logfile framework](https://github.com/KristianVirkus/Logfile) allows logging to files or consoles in a structured fashion. Different information retain their types throughout the whole logging process and even make it inside the logfile. Thus they can later be parsed easily for automated analysis or displaying purposes. The logging occurs in a textual format which is easily readable by humans.

## File Format

* The file contains structured entities
* Entities are separated by `0x1e`:
  * One header as the first entity preceeded by an entity separator
  * Arbitrary number of events
* All separator characters within the entity data must be escaped
* Each separator follows an entity type identification, it may be separated by whitespaces from the separator character
* All data within the entity followed by the are subject to the entity specifications

## Header

* The header entity type is `SLF.1`
* Header details are separated by `01f`
* Ignore whitespaces around control characters and `-`, `=`, `#`, `*`, `\r`, and `\n` characters after the separator
* Every header contains these details in the first row:
  * `app` The application name, to be defined when initialising the logfile
  * `start-up` The application's start-up time, automatically determined
  * `instance` The ID of the instance, randomly generated
  * `seq-no` The logfile sequence number, automatically generated
* Header details can have arbitrary key-value-pairs which may be separated by new lines
  * Keys and values are wrapped in \` signs
  * \` signs, record separators, details separators, and other control characters except for '\t', '\r', and '\n' must be escaped by appling URI encoding

## Events

* The event entity type is `EVENT`
* Event details are separated by `01f`
* Ignore whitespaces around control characters and `-`, `=`, `#`, `*`, `\r`, and `\n` characters after the separator
* Every event contains these details without detail names (not as key-value-pairs):
  * Time in ISO 8601 format
  * Loglevel text
  * Developer mode event
  * Event ID
* Event details can have arbitrary key-value-pairs which may be separated by new lines
  * Keys and values are wrapped in \` signs
  * \` signs, record separators, details separators, and other control characters except for '\t', '\r', and '\n' must be escaped by appling URI encoding

## Sample output

```
SLF.1
    AppName=`abc`
    AppStartupTime=`2019-01-02T12:00:00Z`
    AppInstanceID=`instance`
    AppInstanceLogfileSequenceNumber=1

EVENT 10:42:31.957 ==  Warning == Dev == 1 Event1 == Message=`multi-line
text`
```