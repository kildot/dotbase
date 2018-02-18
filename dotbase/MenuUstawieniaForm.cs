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
    public partial class MenuUstawieniaForm : Form
    {
        //---------------------------------------------------------
        public MenuUstawieniaForm()
        //---------------------------------------------------------
        {
            InitializeComponent();
            
        }

        //---------------------------------------------------------
        // Budzet niepewnosc
        private void button1_Click(object sender, EventArgs e)
        //---------------------------------------------------------
        {
            BudzetNiepewnosciForm okno = new BudzetNiepewnosciForm();

            if (false == okno.Inicjalizuj())
            {
                MessageBox.Show("Brak odpowiednich danych w bazie do wyświetlenia.");
            }

            okno.ShowDialog();
        }

        //---------------------------------------------------------
        // protokoły kalibracyjne ławy
        private void button2_Click(object sender, EventArgs e)
        //---------------------------------------------------------
        {
            ProtokolyKalibracyjneForm okno = new ProtokolyKalibracyjneForm();
            
            if (false == okno.Inicjalizuj())
            {
                MessageBox.Show("Brak odpowiednich danych w bazie do wyświetlenia.");
            }

            okno.ShowDialog();
        }

        //---------------------------------------------------------
        // wzorcowanie źródeł
        private void button3_Click(object sender, EventArgs e)
        //---------------------------------------------------------
        {
            WzorcowanieZrodelForm okno = new WzorcowanieZrodelForm();
            
            if (false == okno.Inicjalizuj())
            {
                MessageBox.Show("Brak odpowiednich danych w bazie do wyświetlenia.");
            }

            okno.ShowDialog();
        }

        //---------------------------------------------------------
        // stałe
        private void button4_Click(object sender, EventArgs e)
        //---------------------------------------------------------
        {
            StaleForm okno = new StaleForm();

            if (false == okno.Inicjalizuj())
            {
                MessageBox.Show("Brak odpowiednich danych w bazie do wyświetlenia.");
            }

            okno.ShowDialog();
        }

        //---------------------------------------------------------
        private void button5_Click(object sender, EventArgs e)
        //---------------------------------------------------------
        {
            Close();
        }

    }
}