# LiveboxToZabbix
A tool that monitors Livebox and send data to zabbix. As this tool is targeting people using Livebox, this readme will be in french.

Cet outil utilise le webservice de la Livebox 4 pour extraire toutes les 15 secondes les donn�es techniques du fonctionnement d'une ligne DSL. Les donn�es sont ensuite envoy�es � un serveur/proxy Zabbix.
Un template est �galement fourni pour Zabbix(6.0) dans le dossier ZabbixTemplates.
La configuration de publication est pr�vue pour proposer un unique binaire .net 7 self contained au format linux x64. Un dockerfile est �galement fourni pour conteneuriser les binaires (pratique pour faire tourner l'outil sur un nas synology par ex)

Les donn�es extraites sont le fruit d'une observation du fonctionnement de l'interface d'administration de la livebox 4 sur une ligne ADSL donn�e. 
Il faut noter qu'�tant bas� sur du r�tro ingenering, d'autant plus sur une unique ligne adsl, les donn�es extraites peuvent �tre incompl�tes ou incorrectement interpr�t�es. 
N'�tant pas affili� au fournisseur d'acc�s internet, les sp�ficiations du webservice peuvent � tout moment changer.

## Fichier de configuration 
Lors du d�marrage, le programme �crit un fichier config-sample.xml. Renommer le fichier en config.xml en ajustant le contenu des noeuds en fonction de votre configuration.

```
<?xml version="1.0" encoding="utf-8"?>
<Configuration xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <LiveboxUrl>http://192.168.1.1</LiveboxUrl>
  <LiveboxLogin>admin</LiveboxLogin>
  <LiveboxPassword>P@ssw0rd</LiveboxPassword>
  <ZabbixServer>192.168.1.100</ZabbixServer>
  <ZabbixHostToPopulate>adsl</ZabbixHostToPopulate>
  <ZabbixServerPort>10051</ZabbixServerPort>
</Configuration>
```

Remarques : 
LiveboxUrl : ne pas mettre de / � la fin. Test� en ipv4, probablement fonctionnel en ipv6.
ZabbixServer : ip ou nom d'h�te du serveur ou proxy zabbix, ipv4 ou ipv6(non test�) 
ZabbixHostToPopulate : nom d'hote de livebox dans Zabbix

## Variables d'environnement
En cas de fonctionnement dans Zabbix, voici la liste des �quivalences avec le fichier de configuration :

| Variable d'environnement | Noeud xml |
| --- | --- |
| LTZ_LiveboxUrl | LiveboxUrl |
| LiveboxLogin | LTZ_LiveboxLogin |
| LiveboxPassword | LTZ_LiveboxPassword |
| ZabbixServer | LTZ_ZabbixServer |
| ZabbixHostToPopulate | LTZ_ZabbixHostToPopulate |
| ZabbixServerPort | LTZ_ZabbixServerPort |

## Inclassable

Le syst�me d'envoi au serveur Zabbix est une version l�g�rement modifi�e de https://github.com/yanngg33/Zabbix_Sender pour pouvoir envoyer de multiples items en un seul appel au serveur.
