﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DotBase
{
    public partial class MenuPismaSwiadectwaForm : Form
    {
        int _NumerKarty;
        BazaDanychWrapper _Baza;
        DocumentationPathsLoader _DocumentationPathsLoader;

        public MenuPismaSwiadectwaForm(int numerKarty)
        {
            int nrPisma;
            int rokPisma;
            InitializeComponent();
            _NumerKarty = numerKarty;
            _Baza = new BazaDanychWrapper();
            _DocumentationPathsLoader = new DocumentationPathsLoader();
            var rokWystawienia = dataWystawienia.Value.Year;

            try
            {
                var row = _Baza.TworzTabeleDanych(String.Format("SELECT nr_pisma, Rok_pisma FROM Karta_przyjecia WHERE id_karty = {0}", _NumerKarty)).Rows[0];
                nrPisma = row.Field<int>(0);
                rokPisma = row.Field<int>(1);
                if (nrPisma == 0 || rokPisma == 0)
                {
                    generujNowyNumer();
                }
                else
                {
                    nrPismaNumer.Text = nrPisma.ToString();
                    nrPismaRok.Text = rokPisma.ToString();
                }
            }
            catch
            {
                generujNowyNumer();
            }

            try
            {
                DataTable table = _Baza.TworzTabeleDanych("SELECT Data_wystawienia, Data_wykonania, Autoryzowal, Uwaga, Waznosc_dwa_lata, Poprawa, UwagaMD, UwagaD, UwagaS, UwagaSMD, UwagaSD " +
                String.Format("FROM Swiadectwo WHERE id_karty = {0}", _NumerKarty));

                dataWystawienia.Value = table.Rows[0].Field<DateTime>("Data_wystawienia");
                textBox4.Text = table.Rows[0].Field<String>("Autoryzowal");
                textBox1.Text = table.Rows[0].Field<String>("Uwaga");
                checkBox1.Checked = table.Rows[0].Field<Boolean>("Waznosc_dwa_lata");
                poprawa.Checked = table.Rows[0].Field<Boolean>("Poprawa");
                dataWykonania.Value = table.Rows[0].Field<DateTime>("Data_wykonania");
                uwMD.Text = table.Rows[0].Field<String>("UwagaMD");
                uwD.Text = table.Rows[0].Field<String>("UwagaD");
                uwS.Text = table.Rows[0].Field<String>("UwagaS");
                uwSMD.Text = table.Rows[0].Field<String>("UwagaSMD");
                uwSD.Text = table.Rows[0].Field<String>("UwagaSD");
            }
            catch (Exception)
            { }

            oswierzCzescStalaNrPisma();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            generujSwiadectwo(Jezyk.PL);
        }

        private void generujSwiadectwo(Jezyk jezykSwiadectwa)
        {
            Program.zmienJezyk(jezykSwiadectwa);
            string sciezka = _DocumentationPathsLoader.GetPath("SwiadectwoWynik", jezykSwiadectwa) + _NumerKarty + poprawaSuffix() + "SwiadectwoWynik";
            sciezka += JezykTools.kocowka(jezykSwiadectwa);
            sciezka += ".html";

            DataTable table = _Baza.TworzTabeleDanych("SELECT Data_przyjecia " +
                "FROM Zlecenia INNER JOIN Karta_przyjecia " +
                "ON Zlecenia.ID_zlecenia = Karta_przyjecia.ID_zlecenia " +
                "WHERE Karta_przyjecia.ID_karty=?", _NumerKarty);

            try
            {
                
                Dokumenty.Swiadectwo swiadectwo = new Dokumenty.Swiadectwo(_NumerKarty,
                                                                           dataWystawienia.Value,
                                                                           dataWykonania.Value,
                                                                           table.Rows[0].Field<DateTime>("Data_przyjecia"),
                                                                           textBox4.Text,
                                                                           poprawa.Checked,
                                                                           uwMD.Text,
                                                                           uwD.Text,
                                                                           uwS.Text,
                                                                           uwSMD.Text,
                                                                           uwSD.Text,
                                                                           jezykSwiadectwa);
                var dane = swiadectwo.pobierzDaneSzablonu();
                if (dane != null)
                {
                    dane.Generate(this);
                }
                else
                {
                    MessageBox.Show("Nie istnieją dane z których można by sporządzić świadectwo.", "Uwaga!");
                }
                
                if (swiadectwo.UtworzDokument(sciezka, dolaczTabPunktyBox.Checked))
                {
                    System.Diagnostics.Process.Start(sciezka);
                }
                else
                {
                    MessageBox.Show("Nie istnieją dane z których można by sporządzić świadectwo.", "Uwaga!");
                }
            }
            finally
            {
                Program.zmienJezyk(Jezyk.PL);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int nrPisma, rokPisma;
            if (false == int.TryParse(nrPismaNumer.Text, out nrPisma))
            {
                MessageBox.Show("Nie podano numeru pisma! Lub numer pisma nie jest liczbą naturalną!", "Błąd!");
                return;
            }
            if (false == int.TryParse(nrPismaRok.Text, out rokPisma))
            {
                MessageBox.Show("Nie podano roku dla numeru pisma! Lub numer pisma nie jest liczbą naturalną!", "Błąd!");
                return;
            }

            _Baza.Karta_przyjecia
                .UPDATE()
                    .Nr_pisma((int)nrPisma)
                    .Rok_pisma((int)rokPisma)
                .WHERE()
                    .ID_karty(_NumerKarty)
                .INFO("Zmieniono nr i rok pisma w karcie przyjęcia")
                .EXECUTE();

            string sciezka = _DocumentationPathsLoader.GetPath("PismoPrzewodnieWynik", Jezyk.PL) + nrPisma + poprawaSuffix() + "PismoPrzewodnieWynik" + _NumerKarty + ".html";

			Dokumenty.PismoPrzewodnie pismo = new Dokumenty.PismoPrzewodnie(_NumerKarty, dataWystawienia.Value, dataWykonania.Value, textBox1.Text, nrPismaNumer.Text, nrPismaRok.Text, checkBox1.Checked, poprawa.Checked, odlaczWykresBox.Checked);
            if (!pismo.generateDocument(sciezka))
            {
                MessageBox.Show("Nie można stowrzyć dokumentu z powodu braku danych lub ich błędnych wartości.", "Uwaga");
            }
        }

        private string poprawaSuffix()
        {
            return poprawa.Checked ? "P" : "";
        }

        private void WstawZnakSpecjalny(object sender, KeyEventArgs e)
        {
            TextBox uwTextBox = (TextBox)sender;

            int i = uwTextBox.SelectionStart;

            if (e.Alt && 77 == e.KeyValue)
            {    
                const string stringDoWstawienia = "&mu;";
                int dlugoscWstawienia = stringDoWstawienia.Length;

                uwTextBox.Text = uwTextBox.Text.Insert(uwTextBox.SelectionStart, stringDoWstawienia);
                uwTextBox.SelectionStart = i + dlugoscWstawienia;
            }
            else if (e.Alt && 84 == e.KeyValue)
            {
                const string stringDoWstawienia = "&nbsp;";
                int dlugoscWstawienia = stringDoWstawienia.Length;

                uwTextBox.Text = uwTextBox.Text.Insert(uwTextBox.SelectionStart, stringDoWstawienia);
                uwTextBox.SelectionStart = i + dlugoscWstawienia;
            }
            else if (e.Alt && 66 == e.KeyValue)
            {
                i = uwTextBox.SelectionStart;
                int i2 = i + uwTextBox.SelectionLength;

                uwTextBox.Text = uwTextBox.Text.Insert(i2, "</b>");
                uwTextBox.Text = uwTextBox.Text.Insert(i, "<b>");

                uwTextBox.SelectionStart = i2 + 7;
            }
            else if (e.Alt && 80 == e.KeyValue)
            {
                const string stringDoWstawienia = "<br>";
                int dlugoscWstawienia = stringDoWstawienia.Length;

                uwTextBox.Text = uwTextBox.Text.Insert(uwTextBox.SelectionStart, stringDoWstawienia);
                uwTextBox.SelectionStart = i + dlugoscWstawienia;
            }
            else if (e.Alt && 68 == e.KeyValue)
            {
                i = uwTextBox.SelectionStart;
                int i2 = i + uwTextBox.SelectionLength;

                uwTextBox.Text = uwTextBox.Text.Insert(i2, "</sub>");
                uwTextBox.Text = uwTextBox.Text.Insert(i, "<sub>");

                uwTextBox.SelectionStart = i2 + 11;
            }
            else if (e.Alt && 71 == e.KeyValue)
            {
                i = uwTextBox.SelectionStart;
                int i2 = i + uwTextBox.SelectionLength;

                uwTextBox.Text = uwTextBox.Text.Insert(i2, "</sup>");
                uwTextBox.Text = uwTextBox.Text.Insert(i, "<sup>");

                uwTextBox.SelectionStart = i2 + 11;
            }
        }

        private void ZamykanieOkna(object sender, FormClosingEventArgs e)
        {
            e.Cancel = !N.PotwierdzenieZapisz(this, ZapiszDane, true, false);
        }

        private bool ZapiszDane()
        {
            int nrPisma, rokPisma;
            if (textBox4.Text == "" || false == Int32.TryParse(nrPismaNumer.Text, out nrPisma) || false == Int32.TryParse(nrPismaNumer.Text, out rokPisma))
            {
                return false;
            }

            if (0 == _Baza.TworzTabeleDanych(String.Format("SELECT 1 FROM Swiadectwo WHERE id_karty = {0}", _NumerKarty)).Rows.Count)
            {
                _Baza.Swiadectwo
                    .INSERT()
                        .Id_karty(_NumerKarty)
                        .Data_wystawienia(dataWystawienia.Value)
                        .Data_wykonania(dataWykonania.Value)
                        .Autoryzowal(textBox4.Text)
                        .Uwaga(textBox1.Text)
                        .Waznosc_dwa_lata(checkBox1.Checked)
                        .Poprawa(poprawa.Checked)
                        .UwagaMD(uwMD.Text)
                        .UwagaD(uwD.Text)
                        .UwagaS(uwS.Text)
                        .UwagaSMD(uwSMD.Text)
                        .UwagaSD(uwSD.Text)
                    .INFO("Dodanie nowego świadectwa")
                    .EXECUTE();
            }
            else
            {
                _Baza.Swiadectwo
                    .UPDATE()
                        .Data_wystawienia(dataWystawienia.Value)
                        .Data_wykonania(dataWykonania.Value)
                        .Autoryzowal(textBox4.Text)
                        .Uwaga(textBox1.Text)
                        .Waznosc_dwa_lata(checkBox1.Checked)
                        .Poprawa(poprawa.Checked)
                        .UwagaMD(uwMD.Text)
                        .UwagaD(uwD.Text)
                        .UwagaS(uwS.Text)
                        .UwagaSMD(uwSMD.Text)
                        .UwagaSD(uwSD.Text)
                    .WHERE()
                        .Id_karty(_NumerKarty)
                    .INFO("Zapis danych świadectwa")
                    .EXECUTE();
            }
            return true;
        }

        private void zapiszToolStripMenuItem_Click(object sender, EventArgs e)
        {
            N.PotwierdzenieZapisz(this, ZapiszDane, false, true);
        }

        private void swiadectEnBtn_Click(object sender, EventArgs e)
        {
            generujSwiadectwo(Jezyk.EN);
        }

        private void poprawa_CheckedChanged(object sender, EventArgs e)
        {
            oswierzCzescStalaNrPisma();
        }

        private void oswierzCzescStalaNrPisma()
        {
            nrPismaStalaCzesc.Text = poprawaSuffix() + "/W/LWPD/";
        }

        private void nrPismaPrzycisk_Click(object sender, EventArgs e)
        {
            if (nrPismaRok.Text.Trim() == dataWystawienia.Value.Year.ToString())
            {
                MessageBox.Show(this, "Rok z daty wystawienia się nie zmienił. Nie ma potrzeby podownego generowania numeru dla tego roku", "Informacja", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                generujNowyNumer();
            }
        }

        private void generujNowyNumer()
        {
            int nrPisma, rokPisma;
            rokPisma = dataWystawienia.Value.Year;
            try
            {
                nrPisma = _Baza.TworzTabeleDanych("SELECT nr_pisma FROM Karta_przyjecia WHERE Rok_pisma=? AND ID_karty=?", rokPisma, _NumerKarty).Rows[0].Field<int>(0);
            }
            catch
            {
                try
                {
                    nrPisma = _Baza.TworzTabeleDanych("SELECT MAX(nr_pisma) FROM Karta_przyjecia WHERE Rok_pisma=?", rokPisma).Rows[0].Field<int>(0) + 1;
                }
                catch
                {
                    nrPisma = 1;
                }
            }
            nrPismaNumer.Text = nrPisma.ToString();
            nrPismaRok.Text = rokPisma.ToString();
            oswierzCzescStalaNrPisma();
        }
        
    }
}