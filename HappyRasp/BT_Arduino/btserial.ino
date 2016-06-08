#include <SoftwareSerial.h>

SoftwareSerial BTSerial(10, 11); // RX | TX

const int VERDE = 12;  
const int BLU = 13;  
const int ROSSO = 9;  


void setup()
{
  pinMode(9, OUTPUT);  // this pin will pull the HC-05 pin 34 (key pin) HIGH to switch module to AT mode
  digitalWrite(9, HIGH);
  Serial.begin(9600);
  //Serial.println("Enter AT commands:");
  BTSerial.begin(9600);  // HC-05 default speed in AT command more
  Serial.println("Setup End");

  delay(100);
   // imposta il pin digitale come output  
  pinMode(VERDE, OUTPUT);  
  pinMode(BLU, OUTPUT);  
  pinMode(ROSSO, OUTPUT);   


  digitalWrite(VERDE, HIGH);  
  digitalWrite(BLU, HIGH);  
  digitalWrite(ROSSO, HIGH);  
  
}

void loop()
{

  char ch1; // bit verde
  char ch2; // bit blu
  char ch3; // bit rosso

  // Keep reading from HC-05 and send to Arduino Serial Monitor
  if(BTSerial.available())
  {
   delay(10);
   ch1 = BTSerial.read();
   delay(10);
   ch2 = BTSerial.read();
   delay(10);
   ch3 = BTSerial.read();
   delay(10);


  if(ch1=='1')
     digitalWrite(VERDE, HIGH);  
  else
   digitalWrite(VERDE, LOW);  

  if(ch2=='1')
     digitalWrite(BLU, HIGH);  
  else
   digitalWrite(BLU, LOW);  
  if(ch3=='1')
     digitalWrite(ROSSO, HIGH);  
  else
   digitalWrite(ROSSO, LOW);  

   Serial.print(ch1);
   Serial.print(ch2);
   Serial.print(ch3);


String stringOne =  String(ch1+ch2+ch3); 
Serial.println(stringOne); 
 
BTSerial.write(ch1);
BTSerial.write(ch2);
BTSerial.write(ch3);
   
    
/*        Serial.print(BTSerial.read());  
         Serial.print(BTSerial.read());  
          Serial.print(BTSerial.read());  
           Serial.print(BTSerial.read());  
            Serial.print(BTSerial.read());  
             Serial.print(BTSerial.read());  
              Serial.print(BTSerial.read());  
               Serial.print(BTSerial.read());  
    if(ch=='a')    
    BTSerial.write("Hello World");
    else
    BTSerial.write("Hello World2");
   // BTSerial.write("Hello World");
while(true)
{
  delay(500);  
   BTSerial.write("Hello World");
   delay(500);  
    BTSerial.write("Hello World2");
       delay(500);  
    BTSerial.write("AAANN nnnn");
}*/

  }

  
}
