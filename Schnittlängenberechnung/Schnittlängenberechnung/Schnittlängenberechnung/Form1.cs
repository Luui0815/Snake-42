using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Schnittlängenberechnung
{
    public partial class Form1 : Form
    {
        private int[] Leisten;
        private int[] LeistenAlt; // wird benötigt, da Leisten beim Baumtraversieren aufgebraucht wird
        private List<int> Reststangen = new List<int>(); // Liste aller Stangen die übrig geblieben sind
        private bool[] LeistenBenutzt;
        private Knoten Baumwurzel;
        private int Durchläufe;
        private int Gesamtrest;
        private double Genauigkeit;
        private bool LeistenangabenGeändert = false;
        private Knoten BesterBaumpfad = null;
        private class Knoten
        {
            public int Leiste; // Länge der Stange in mm
            public Knoten Vorgängerknoten;
            public List<Knoten> Folgeknoten; // alle möglichen Nachfolgerknoten
            public int VerbrauchteLänge; // bis dahin benutze Rohmateriallänge
            public bool[] VerwendeteLeisten; // bis dahin verwendete Stangen
            public Knoten(Knoten Vorgänger, int Leistenlänge, bool[]BenutzteLeisten)
            {
                Vorgängerknoten = Vorgänger;
                Leiste = Leistenlänge;
                Folgeknoten = new List<Knoten>(); // Folgeknoten müssen im nachhinein angehängt werden
                if (Vorgänger != null)
                    VerbrauchteLänge = Leistenlänge + Vorgänger.VerbrauchteLänge;
                else
                    VerbrauchteLänge = Leistenlänge;

                VerwendeteLeisten = new bool[BenutzteLeisten.Length]; // die Stange im aktuellen Knoten sollte darin schon enthalten sein
                // for Schleife um Werte in LeistenBenutzt auf AktKnoten.VerwendeteLeisten zurückzusetzen
                // bei LeistenBenutzt = AktKnoten.VerwendeteLeisten wird referenz übergeben, das nicht gut
                for (int i = 0; i < BenutzteLeisten.Length; i++)
                    VerwendeteLeisten[i] = BenutzteLeisten[i];
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void bt_berechne_Click(object sender, EventArgs e)
        {
            if (Convert.ToInt32(tb_Maximallänge.Text) <= Convert.ToInt32(tb_LängeStangen.Text))
            {
                //Reststangen.Clear();
                lb_erzeugteStangen.Items.Clear();
                // wenn nicht bereits neue Leistenlängen genriert wurden, neu generieren
                if (LeistenAlt == null || LeistenAlt.Length != Convert.ToInt32(tb_AnzLeisten.Text) || LeistenangabenGeändert == true)
                {
                    ZufälligeLeistenlängenGenerieren();
                    LeistenangabenGeändert = false;
                }
                Leisten = new int[LeistenAlt.Length];
                LeistenBenutzt = new bool[LeistenAlt.Length];
                // muss so, bei Leisten = LeistenAlt wird nur ein Zeiger übergeben und somit ändert sich LeistenAlt wenn sich Leisten ändert
                for(int i = 0; i < LeistenAlt.Length; i++)
                {
                    Leisten[i] = LeistenAlt[i];
                    LeistenBenutzt[i] = false;
                }

                ListboxStangenlängenSchreiben();
                Durchläufe = 1;
                Gesamtrest = 0;
                Genauigkeit = Convert.ToInt32(tb_Genauigkeit.Text) / 100.0;

                int VerwendeteStangengesamtlänge = 0; // Länge aller Stangen die verwendet wurden, ohne Abzug des Restes für Fazit
                while (Leisten.Length != 0)
                {
                    int Stange;
                    // Ziel genau 1 Baum aufspannen aus dem aufgebauten Baum genau 1 optimalen Pfad berechnen
                    // und alle die benutzt wurden löschen, das gleiche wieder bis es keine Leisten mehr gibt/alle verarbeitet wurden

                    // erst prüfen ob man eine Resttange verwenden kann
                    Stange = ReststangeVerwendbar();
                    if (Stange != 0)
                    {
                        // Reststange aus Resttangen löschen
                        Reststangen.Remove(Stange);
                        // von Gesmtrest den Stangenrest abziehen, da er doch noch benutzt wird
                        if(!lb_Reststangen.Items.Contains(Stange))
                            Gesamtrest -= Stange;
                        // Baum ist schon aufgebaut, nur noch durchlaufen
                        Baumpfadfindung(Stange);
                        VerwendeteStangengesamtlänge += Stange;
                    }
                    else
                    {
                        BaumAufbauen(Convert.ToInt32(tb_LängeStangen.Text));
                        Baumpfadfindung(Convert.ToInt32(tb_LängeStangen.Text));
                        VerwendeteStangengesamtlänge += Convert.ToInt32(tb_LängeStangen.Text);
                    }

                    Durchläufe++;

                }
                lb_fazit.Text = "Von insgesamt " + VerwendeteStangengesamtlänge + " mm Stange wurden insgesamt " + Gesamtrest + " mm verschwendet";
                AktualisiereLBReststangen();
            }
            else
                MessageBox.Show("Maximallänge ist größer als Länge des Rohmaterials");

        }
        private void AktualisiereLBReststangen()
        {
            lb_Reststangen.Items.Clear();
            foreach (int Stange in Reststangen)
            {
                lb_Reststangen.Items.Add(Stange);
            }
        }

        private int ReststangeVerwendbar()
        {
            foreach (int Reststange in Reststangen)
            {
                Baumwurzel = null;
                BaumAufbauen(Reststange);
                if (Baumwurzel != null)
                {
                    return Reststange;
                }
            }
            return 0;
        }

        private void ZufälligeLeistenlängenGenerieren()
        {
            LeistenAlt = new int[Convert.ToInt32(tb_AnzLeisten.Text)];
            LeistenBenutzt = new bool[Convert.ToInt32(tb_AnzLeisten.Text)];

            Random r = new Random();
            lb_Stangenlängen.Items.Clear();

            for (int i = 0; i < LeistenAlt.Length; i++)
            {
                LeistenAlt[i] = r.Next(Convert.ToInt32(tb_Mindestlänge.Text), Convert.ToInt32(tb_Maximallänge.Text));
                LeistenBenutzt[i] = false;
            }
        }

        private void ListboxStangenlängenSchreiben()
        {
            lb_Stangenlängen.Items.Clear();
            LeistenAlt = SortiereStangen(LeistenAlt);
            // auf Listbox schreiben
            for (int i = 0; i < LeistenAlt.Length; i++)
                lb_Stangenlängen.Items.Add("Leiste " + (i + 1) + ": " + LeistenAlt[i] + " mm");
        }

        private int[] SortiereStangen(int[] Werte)
        {
            // Sortierung der Größe nach absteigenden Längen durch Bubblesort
            for (int j = 0; j <= Werte.Length - 2; j++)
            {
                for (int i = 0; i <= Werte.Length - 2; i++)
                {
                    if (Werte[i] < Werte[i + 1])
                    {
                        int temp = Werte[i + 1];
                        Werte[i + 1] = Werte[i];
                        Werte[i] = temp;
                    }
                }
            }
            return Werte;
        }
            

        private void BaumAufbauen(int Stangenlänge)
        {
            Leisten = SortiereStangen(Leisten);

            Knoten AktBaumknoten=null;
            int Restlänge=0;

            // Startleiste für den Baum suchen
            // kann denke ich auch einfach Index 0 von Leistenn sein
            for (int i= 0; i < Leisten.Length; i++)
            {
                if(LeistenBenutzt[i] == false && Leisten[i] <= Stangenlänge)
                {
                    LeistenBenutzt[i] = true;
                    Baumwurzel = new Knoten(null, Leisten[i], LeistenBenutzt);
                    AktBaumknoten = Baumwurzel;
                    Restlänge = Stangenlänge - AktBaumknoten.Leiste;
                    // erstmal den Baum "Einspurig" aufbauen bis Restlänge < 25% Gesamtlänge
                    break;
                }
            }

            //wenn kein Wurzelknoten gefunden wurde ist die Stange kleiner als die kleinste Leiste, return
            if (Baumwurzel == null)
                return;

            // den Baum bist 75% Materialverbrauch einspurig aufspannen
            int index = 0;
            while (Restlänge < (1.0 - Genauigkeit) * Stangenlänge && !AlleStangenBenutzt())
            {
                if (Restlänge - Leisten[index] >= 0 && LeistenBenutzt[index] == false)
                {
                    // Nachfolgeknoten erstellen
                    LeistenBenutzt[index] = true;
                    Knoten Nachfolger = new Knoten(AktBaumknoten, Leisten[index], LeistenBenutzt);
                    AktBaumknoten.Folgeknoten.Add(Nachfolger);
                    Restlänge -= Leisten[index];
                    index++;
                    AktBaumknoten = Nachfolger;
                }
                else
                    index++;

                if (index >= Leisten.Length)
                {
                    // wenn hier rein gesprungen wird, kann man mit den vorhandenen Leisten nicht 75% der Materiallänge erreichen
                    // d.h. alle Leisten sind hier bereits verbraucht und der Baum ist fertig
                    return;
                }
            }

            // ab hier werden alle Möglichkeiten in den Baum hinzugefügt
            // solange immer links in den Baum absteigen und die Baumebene aufspannen bis die Nachfolgeknoten alle null sind
            BaumVerzweigenLassen(AktBaumknoten,Stangenlänge);
        }

        private void BaumVerzweigenLassen(Knoten AktBaumknoten, int Stangenlänge)
        {
            AufspannenDerAktuellenBaumebene(AktBaumknoten, Stangenlänge);
            if(AktBaumknoten.Folgeknoten.Count == 0)
            {
                return;
            }
            foreach(Knoten k in AktBaumknoten.Folgeknoten)
            {
                BaumVerzweigenLassen(k, Stangenlänge);
            }
        }

        private void AufspannenDerAktuellenBaumebene(Knoten AktKnoten, int Stangenlänge) // in horizontaler Richtung!
        {
            int Restlänge = Stangenlänge - AktKnoten.VerbrauchteLänge;

            for(int i = 0; i<Leisten.Length; i++)
            {
                // for Schleife um Werte in LeistenBenutzt auf AktKnoten.VerwendeteLeisten zurückzusetzen
                // bei LeistenBenutzt = AktKnoten.VerwendeteLeisten wird referenz übergeben, das nicht gut
                for (int j = 0; j < LeistenBenutzt.Length; j++)
                    LeistenBenutzt[j] = AktKnoten.VerwendeteLeisten[j];

                if (Restlänge - Leisten[i] >= 0 && LeistenBenutzt[i] == false)
                {
                    LeistenBenutzt[i] = true;
                    Knoten Nachfolger = new Knoten(AktKnoten, Leisten[i], LeistenBenutzt);
                    AktKnoten.Folgeknoten.Add(Nachfolger);
                }
            }
        }

        private bool AlleStangenBenutzt()
        {
            for(int i = 0; i<Leisten.Length; i++)
            {
                if (LeistenBenutzt[i] == false)
                    return false;
            }
            return true;
        }

        private void Baumpfadfindung(int Stangenlänge)
        {
            BesterBaumpfad = null;
            Baumtraversierung(Baumwurzel, Stangenlänge);
            Baumpfadauswertung(Stangenlänge);
        }

        private void Baumtraversierung(Knoten AktKnoten, int Stangenlänge)
        {
            if(AktKnoten.Folgeknoten.Count == 0)
            {
                if (BesterBaumpfad == null)
                    BesterBaumpfad = AktKnoten;
                else
                {
                    if (BesterBaumpfad.VerbrauchteLänge < AktKnoten.VerbrauchteLänge)
                        BesterBaumpfad = AktKnoten;
                }
                return;
            }

            foreach(Knoten k in AktKnoten.Folgeknoten)
            {
                Baumtraversierung(k, Stangenlänge);
            }
        }

        private void Baumpfadauswertung(int Stangenlänge)
        {
            // Text für Listbox generieren
            Knoten AktKnoten = BesterBaumpfad;
            string Text = "Stange " + Durchläufe + " (" + Stangenlänge + " mm lange Stange)" + ": ";

            while (AktKnoten != null)
            {
                Text += AktKnoten.Leiste + ", ";
                // Leisten die benutzt wurden auf 0 setzen
                for (int i = 0; i < Leisten.Length; i++)
                {
                    if (Leisten[i] == AktKnoten.Leiste)
                    {
                        Leisten[i] = 0;
                        break;
                    }
                }
                AktKnoten = AktKnoten.Vorgängerknoten;
            }

            Gesamtrest += (Stangenlänge - BesterBaumpfad.VerbrauchteLänge);
            Text += "restliche Stange: " + (Stangenlänge - BesterBaumpfad.VerbrauchteLänge);
            // Stangen den Reststangen hinzufügen, wenn größer Restmülllänge
            if (Stangenlänge - BesterBaumpfad.VerbrauchteLänge > Convert.ToInt32(tb_restmüllgrenze.Text))
                Reststangen.Add(Stangenlänge - BesterBaumpfad.VerbrauchteLänge);

            lb_erzeugteStangen.Items.Add(Text);

            // alle verbrauchten Leisten aus dem Leisten-Array rauswerfen
            int ZählVar = 0;
            int[] NeueLeisten = new int[Leisten.Length];
            int NeuLeistenIndex = 0;
            for (int i = 0; i < Leisten.Length; i++)
            {
                if (Leisten[i] != 0)
                {
                    ZählVar++;
                    NeueLeisten[NeuLeistenIndex] = Leisten[i];
                    NeuLeistenIndex++;
                }
            }

            Leisten = new int[ZählVar];
            LeistenBenutzt = new bool[ZählVar];

            for (int i = 0; i < Leisten.Length; i++)
            {
                Leisten[i] = NeueLeisten[i];
                LeistenBenutzt[i] = false;
            }
        }

        private void bt_Zufallsleisten_Click(object sender, EventArgs e)
        {
            ZufälligeLeistenlängenGenerieren();
            Leisten = LeistenAlt;
            ListboxStangenlängenSchreiben();
            lb_erzeugteStangen.Items.Clear();
        }

        private void tb_Mindestlänge_TextChanged(object sender, EventArgs e)
        {
            LeistenangabenGeändert = true;
        }

        private void tb_Maximallänge_TextChanged(object sender, EventArgs e)
        {
            LeistenangabenGeändert = true;
        }

        private void lb_Reststangen_DoubleClick(object sender, EventArgs e)
        {
            if(!(lb_Reststangen.SelectedIndex > Reststangen.Count) && lb_Reststangen.SelectedIndex != -1)
                Reststangen.RemoveAt(lb_Reststangen.SelectedIndex);
            AktualisiereLBReststangen();
        }
    }
}
