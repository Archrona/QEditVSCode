
import * as vscode from 'vscode';
import * as request from 'request';
import * as express from 'express';
import * as path from 'path';

const insertionMarker = {
	left: "▻",
	right: "◅"
};

function getEditor(): vscode.TextEditor | undefined {
	let editor = vscode.window.activeTextEditor;
	return editor;
}

function createInsertionPoint(): void {
	let editor = getEditor();
	if (!editor) {
		return;
	}

	let document = editor.document;
	let selection = editor.selection;
	let word = document.getText(selection);

	editor.edit(editBuilder => {
		editBuilder.replace(
			selection,
			insertionMarker.left + word + insertionMarker.right
		);
	});

	editor.selections = [new vscode.Selection(
		new vscode.Position(selection.end.line, selection.end.character + 1),
		new vscode.Position(selection.end.line, selection.end.character + 1)
	)];

	
	const extension = path.extname(document.fileName);

	request({
		method: 'POST',
		uri: 'http://localhost:3141/clear?ext=' + extension,
		timeout: 100
	}, (err, response, body) => {
		if (err) {
			console.error(err);
		} else {
			console.log(response.statusCode);
			console.log(body);
		}
	}).on("error", (err) => {
		console.log(err);
	});
}

function findInsertionMarkers(doc: vscode.TextDocument) {
	if (doc.getText().length > 1000000) {
		return [];
	}

	if (doc.lineCount <= 0) {
		return [];
	}

	let markers = [];
	let line = 0, col = 0;
	let text = doc.lineAt(line);
	let left = null, right = null;

	while (line < doc.lineCount) {
		if (col >= text.text.length) {
			line++;
			if (line >= doc.lineCount) {
				break;
			}
			text = doc.lineAt(line);
			col = 0;
		} else {
			let char = text.text.charAt(col);

			if (char === insertionMarker.left) {
				left = new vscode.Position(line, col);
			}
			if (char === insertionMarker.right) {
				right = new vscode.Position(line, col + 1);
			}

			if (left !== null && right !== null && left.compareTo(right) < 0) {
				markers.push(new vscode.Range(left, right));
				left = null;
				right = null;
			}

			col++;
		}
	}

	return markers;
}

function updateInsertionPoint(code: string, final: boolean) {
	const editor = getEditor();
	if (editor) {
		const document = editor.document;
		const markers = findInsertionMarkers(document);
		let found = -1;

		if (markers.length > 0) {
			for (let i = 0; i < markers.length; i++) {
				if (markers[i].intersection(editor.selection) !== undefined) {
					found = i;
					break;
				}
			}
		}

		if (found !== -1) {
			const markerToChange = markers[found];
			const nonwhiteIndex = document.lineAt(markerToChange.start.line)
				.firstNonWhitespaceCharacterIndex;
			const leftIndent = document.getText(new vscode.Range(
				new vscode.Position(markerToChange.start.line, 0),
				new vscode.Position(markerToChange.start.line, nonwhiteIndex)
			));
			console.log("Changing marker \"" + document.getText(markerToChange) + "\"");
			
			const lines = code.split(/\r?\n/);
			let indentedCode = lines[0];
			for (let i = 1; i < lines.length; i++) {
				indentedCode += "\n" + leftIndent + lines[i];
			}


			let targetText = 
				(final ? "" : insertionMarker.left)
				+ indentedCode
				+ (final ? "" : insertionMarker.right);
			
			const pos = new vscode.Position(
				markerToChange.end.line,
				markerToChange.end.character - 1
			);

			editor.selection = new vscode.Selection(pos, pos);

			editor.edit((eb) => {
				eb.replace(
					markerToChange,
					targetText
				);
			});

			
		} else {
			console.log("No active marker");
		}
	}
}

function createServer() {
	const app = express();
	app.use(express.json());
	app.post('/', (req, res) => {
		if (req.body !== undefined && req.body.text !== undefined && req.body.final !== undefined) {
			updateInsertionPoint(req.body.text, req.body.final);
		}
		res.send("Successful!");
	});
	app.listen(3145, () => {
		console.log("Listening on port 3145");
	});

}

export function activate(context: vscode.ExtensionContext) {
	console.log("Qedit: Activated");

	context.subscriptions.push(
		vscode.commands.registerCommand(
			'extension.createInsertionPoint', 
			createInsertionPoint
		)
	);

	createServer();

	//setInterval(tick, 250);
}

export function deactivate() {
	console.log("Qedit: Deactivated");
}
