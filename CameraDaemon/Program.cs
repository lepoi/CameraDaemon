using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using CameraControl.Devices;
using CameraControl.Devices.Classes;
using System.Data;
using System.Text.RegularExpressions;

namespace CameraDaemon {
	public partial class Program {
		public string TargetPath = "";
		public string TargetStudent = "";
		public string StudentList = "";
		private CameraDeviceManager Manager;

		static void Main(string[] args) {
			Program program = new Program();

			while (true) {
				string Temp = Console.ReadLine();

				if (Temp.Contains(".csv")) {
					program.StudentList = Temp;

					string[] PathArr = Temp.Split('\\');
					PathArr = PathArr.Take(PathArr.Length - 1).ToArray<string>();
					program.TargetPath = String.Join("\\", PathArr);
				}
				else
					program.TargetStudent = Temp;
			}
		}

		public Program() {
			Manager = new CameraDeviceManager();

			Manager.PhotoCaptured += PhotoCapturedWrapper;
			Manager.CameraConnected += CameraConnected;
			Manager.CameraDisconnected += CameraDisconnected;

			Manager.ConnectToCamera();
		}

		public static DataTable ConvertCSVtoDataTable(string strFilePath) {	
			StreamReader sr = new StreamReader(strFilePath, System.Text.Encoding.Unicode);
			string[] headers = sr.ReadLine().Split(',');
			DataTable dt = new DataTable();
			foreach (string header in headers) {
				dt.Columns.Add(header);
			}
			while (!sr.EndOfStream) {
				string[] rows = Regex.Split(sr.ReadLine(), ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");
				DataRow dr = dt.NewRow();
				for (int i = 0; i < headers.Length; i++)
					dr[i] = rows[i];

				dt.Rows.Add(dr);
			}
			return dt;
		}

		private void CameraConnected(object Event) {
			Manager.ConnectToCamera();
			Console.WriteLine("Camera connceted:" + Event.ToString());
		}

		private void CameraDisconnected(object Event) {
			Console.WriteLine("Camera disconnected");
		}

		private void PhotoCapturedWrapper(object Sender, PhotoCapturedEventArgs EventArgs) {
			Thread NewThread = new Thread(PhotoCaptured);
			NewThread.Start(EventArgs);
		}

		private void PhotoCaptured(object O) {
			PhotoCapturedEventArgs EventArgs = O as PhotoCapturedEventArgs;

			if (EventArgs == null || TargetPath == "" || TargetStudent == "")
				return;

			try {
				string TargetDirectory = TargetPath + "\\Originales con ID";
				string FileName = TargetDirectory + "\\" + TargetStudent;
				uint Counter = 0;

				if (!Directory.Exists(TargetDirectory))
					Directory.CreateDirectory(TargetDirectory);

				while (File.Exists(FileName + ".jpg"))
					FileName = TargetPath + "\\Originales con ID\\" + TargetStudent + "(" + (Counter++) + ")";

				EventArgs.CameraDevice.TransferFile(EventArgs.Handle, FileName + ".jpg");
				EventArgs.CameraDevice.IsBusy = false;

				Console.WriteLine("Photo captured:" + FileName + ".jpg");
			}
			catch (Exception Exception) {
				EventArgs.CameraDevice.IsBusy = false;
				Console.WriteLine("Error descargando foto [" + Exception.GetType() + "]:\n" + Exception.Message);
			}
		}
	}
}
