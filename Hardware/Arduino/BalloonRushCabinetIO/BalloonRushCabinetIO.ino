/*
  Balloon Rush cabinet I/O bridge
  Target: Arduino Uno-compatible board
  Serial: 115200 baud, newline-delimited ASCII

  IMPORTANT:
  - Use an opto-isolator/transistor driver for the ticket output.
  - Do not power a ticket motor or solenoid directly from an Arduino pin.
  - Verify voltage, polarity, and timing for the actual cabinet hardware.
*/

#include <Arduino.h>

namespace Pins {
  const uint8_t Left = 2;
  const uint8_t Pop = 3;
  const uint8_t Right = 4;
  const uint8_t Start = 5;
  const uint8_t Coin = 6;
  const uint8_t Card = 7;
  const uint8_t Operator = 8;
  const uint8_t Back = 9;
  const uint8_t TicketOutput = 10;
}

const unsigned long DebounceMilliseconds = 28;
const unsigned long TicketPulseOnMilliseconds = 35;
const unsigned long TicketPulseOffMilliseconds = 35;
const bool TicketOutputActiveLow = true;

struct DebouncedInput {
  uint8_t pin;
  const char* message;
  bool stableState;
  bool lastRawState;
  unsigned long changedAt;
};

DebouncedInput inputs[] = {
  { Pins::Left, "LEFT", HIGH, HIGH, 0 },
  { Pins::Pop, "POP", HIGH, HIGH, 0 },
  { Pins::Right, "RIGHT", HIGH, HIGH, 0 },
  { Pins::Start, "START", HIGH, HIGH, 0 },
  { Pins::Coin, "COIN", HIGH, HIGH, 0 },
  { Pins::Card, "CARD", HIGH, HIGH, 0 },
  { Pins::Operator, "OPERATOR", HIGH, HIGH, 0 },
  { Pins::Back, "BACK", HIGH, HIGH, 0 }
};

const size_t InputCount = sizeof(inputs) / sizeof(inputs[0]);

String receiveBuffer;
unsigned long queuedTicketPulses = 0;
bool ticketPulseActive = false;
unsigned long ticketStateChangedAt = 0;

void setTicketOutput(bool active) {
  bool level = TicketOutputActiveLow ? !active : active;
  digitalWrite(Pins::TicketOutput, level ? HIGH : LOW);
}

void setup() {
  Serial.begin(115200);
  receiveBuffer.reserve(64);

  for (size_t i = 0; i < InputCount; ++i) {
    pinMode(inputs[i].pin, INPUT_PULLUP);
    inputs[i].stableState = digitalRead(inputs[i].pin);
    inputs[i].lastRawState = inputs[i].stableState;
    inputs[i].changedAt = millis();
  }

  pinMode(Pins::TicketOutput, OUTPUT);
  setTicketOutput(false);
  Serial.println(F("READY"));
}

void loop() {
  pollInputs();
  pollSerial();
  serviceTicketOutput();
}

void pollInputs() {
  const unsigned long now = millis();
  for (size_t i = 0; i < InputCount; ++i) {
    DebouncedInput& input = inputs[i];
    const bool raw = digitalRead(input.pin);

    if (raw != input.lastRawState) {
      input.lastRawState = raw;
      input.changedAt = now;
    }

    if ((now - input.changedAt) < DebounceMilliseconds || raw == input.stableState) {
      continue;
    }

    input.stableState = raw;
    if (input.stableState == LOW) {
      Serial.println(input.message);
    }
  }
}

void pollSerial() {
  while (Serial.available() > 0) {
    const char value = static_cast<char>(Serial.read());
    if (value == '\r') {
      continue;
    }

    if (value == '\n') {
      receiveBuffer.trim();
      if (receiveBuffer.length() > 0) {
        handleCommand(receiveBuffer);
      }
      receiveBuffer = "";
      continue;
    }

    if (receiveBuffer.length() < 63) {
      receiveBuffer += value;
    }
  }
}

void handleCommand(String command) {
  command.trim();
  command.toUpperCase();

  if (command.startsWith("TICKETS:")) {
    const long requested = command.substring(8).toInt();
    if (requested > 0) {
      const unsigned long safeRequested = static_cast<unsigned long>(requested);
      const unsigned long room = 10000UL - min(queuedTicketPulses, 10000UL);
      queuedTicketPulses += min(safeRequested, room);
      Serial.print(F("TICKET_QUEUE:"));
      Serial.println(queuedTicketPulses);
    }
    return;
  }

  if (command == "PING") {
    Serial.println(F("PONG"));
    return;
  }

  if (command == "CLEAR_TICKETS") {
    queuedTicketPulses = 0;
    ticketPulseActive = false;
    setTicketOutput(false);
    Serial.println(F("TICKET_QUEUE:0"));
    return;
  }

  Serial.print(F("UNKNOWN:"));
  Serial.println(command);
}

void serviceTicketOutput() {
  const unsigned long now = millis();

  if (ticketPulseActive) {
    if ((now - ticketStateChangedAt) >= TicketPulseOnMilliseconds) {
      ticketPulseActive = false;
      ticketStateChangedAt = now;
      setTicketOutput(false);
      if (queuedTicketPulses > 0) {
        --queuedTicketPulses;
      }
    }
    return;
  }

  if (queuedTicketPulses == 0) {
    return;
  }

  if ((now - ticketStateChangedAt) >= TicketPulseOffMilliseconds) {
    ticketPulseActive = true;
    ticketStateChangedAt = now;
    setTicketOutput(true);
  }
}
