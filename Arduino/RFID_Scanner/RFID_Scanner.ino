#include <SPI.h>
#include <MFRC522.h>

#define RST_PIN 9
#define SS_PIN 10

// Create MFRC522 instance
MFRC522 mfrc522(SS_PIN, RST_PIN);

void setup() {
  Serial.begin(9600);   // Initialize serial communications with the PC
  while (!Serial);      // Do nothing if no serial port is opened
  
  SPI.begin();          // Init SPI bus
  mfrc522.PCD_Init();   // Init MFRC522
  
  Serial.println("Eco-Matic RFID Scanner Ready.");
}

void loop() {
  // Look for new cards
  if ( ! mfrc522.PICC_IsNewCardPresent()) {
    return;
  }

  // Select one of the cards
  if ( ! mfrc522.PICC_ReadCardSerial()) {
    return;
  }

  // Print a prefix so your C# app knows this is an RFID scan
  Serial.print("RFID:");

  // Read the UID of the card and print it in Hexadecimal
  for (byte i = 0; i < mfrc522.uid.size; i++) {
    Serial.print(mfrc522.uid.uidByte[i] < 0x10 ? "0" : "");
    Serial.print(mfrc522.uid.uidByte[i], HEX);
  } 
  
  Serial.println(); // Send a newline character to signal the end of the string

  // Halt PICC to stop reading the same card multiple times instantly
  mfrc522.PICC_HaltA();
  
  // Stop encryption on PCD
  mfrc522.PCD_StopCrypto1();
}