const cp = require("node:child_process");
const util = require("node:util");
const exec = util.promisify(cp.exec);
const os = require("node:os")

async function copyToClipboard(text) {
  const platform = os.platform();
  let command = '';

  // 1. Assign the correct OS command
  if (platform === 'darwin') {
    command = 'pbcopy';
  } else if (platform === 'win32') {
    command = 'clip';
  } else if (platform === 'linux') {
    command = 'xclip -selection clipboard || xsel --clipboard --input';
  } else {
    throw new Error(`Unsupported platform: ${platform}`);
  }

  // 2. Start the process execution promise
  const processPromise = exec(command);

  // 3. Write text directly to stdin to prevent shell injection/escaping errors
  processPromise.child.stdin.write(text);
  processPromise.child.stdin.end();

  // 4. Await the execution completion
  await processPromise;
}

(async() => {
    const {stdout, stderr} = await exec("dotnet run -- item --key fBvjN1nXOEOMAlwdT0PnPyLdKTFV6RiTMQnNLcsKCeGYQLE6ZXlKaGJHY2lPaUpTVXpJMU5pSXNJbXRwWkNJNkluTnBaeTB5TURJeExUQTNMVEV6VkRFNE9qVXhPalE1V2lJc0luUjVjQ0k2SWtwWFZDSjkuZXlKaGRXUWlPaUpTYjJKc2IzaEpiblJsY201aGJDSXNJbWx6Y3lJNklrTnNiM1ZrUVhWMGFHVnVkR2xqWVhScGIyNVRaWEoyYVdObElpd2lZbUZ6WlVGd2FVdGxlU0k2SW1aQ2RtcE9NVzVZVDBWUFRVRnNkMlJVTUZCdVVIbE1aRXRVUmxZMlVtbFVUVkZ1VGt4amMwdERaVWRaVVV4Rk5pSXNJbTkzYm1WeVNXUWlPaUkwTnpjNU5EVXlOekVpTENKbGVIQWlPakUzT0RjMU56RTNOVGNzSW1saGRDSTZNVGM0TnpVMk9ERTFOeXdpYm1KbUlqb3hOemczTlRZNE1UVTNmUS5tbW84TC1VRUdmczk4dmk3bnNjYUxhV21lcVJkTm9EMTJneXV0c1JsbWYxRXJzR0lyT00wWTh2bGxEdFQ0MXpQSGFrZVN2WjZRMmlDVmF0U3lmajB5RV9hM2UxZU5lVFRtTWJyWXBOZm1VN0tXeTZYVTVFdjRLa256bUs5YlhOZnhhZjNOZFNiQm5UemV5V2taa0RmSUpNT1pkU19waFZ4R0VoVWtJYlY2SEpBVFV6aERvZEIwQkMxNWtBSXZ1RzlMZjFjdUk0MHhVcUJ5YWpCUXlpbmNrQXY5aVpId1gzVXZxUEl0eXN4TUVCd3AtTExUZm1peGczcTdCRU9yOFBDbmszeVBCcm1oN3R4dngwUldoTU55UVU2bk1CekZLbEdyajNNUVZNOEw4dXBnWENWeVJNTmY5Y216b1dWcEQzaXIwX2hQSGN1Xzk0WmpuZkhYWTV1Smc=::api --item-id 13035715862",  {
        maxBuffer: 1024 * 1024 * 1024
    });
    console.log(stdout)
    copyToClipboard(stdout);
})()