import React, { Component } from 'react';

export class Home extends Component {
  static displayName = Home.name;

  constructor(props) {
    super(props);
      this.state = { chars: [], pattern: '', filtered: [], loading: true };
  }

  componentDidMount() {
      this.fetchChars();
  }
    async find() {
        //alert(this.state.pattern);
        this.setState({ loading: true, error: null });
        try {
            const response = await fetch('dict/search?pattern=' + this.state.pattern);
            const data = await response.json();
            this.setState({ filtered: data, loading: false });
        } catch (ex) {
            this.setState({ filtered: [], loading: false, error: ex });
        }
    }
    renderTable() {
        let filtered = new Map();
        this.state.filtered.forEach(f => {
            let s = filtered.get(f[0]);
            if (!s) {
                filtered.set(f[0], s = []);
            };
            let parts = [f[2], ['r', f[1][0]]];
            for (let i = 3; i < f.length-1; i+=2) {
                parts.push([f[i], f[i + 1]]);
            }
            parts.push(['r', f[1].slice(-1)]);
            s.push(parts);
        });
        let data = Array.from(filtered).map(([a, b]) => [a, Array.from(b)]);
        data.sort((a, b) => a[0] < b[0] ? -1 : 1);
        return (
            <div onKeyDown={ev => { if (ev.keyCode == 32) { ev.preventDefault(); this.setState({ pattern: this.state.pattern.concat(' ') }) } }}>
                <div className="firstRow">
                    <input type="button" value={'Word start (^)'} onClick={_ => this.setState({ pattern: '^' + this.state.pattern })} />
                    <input type="button" value={' '} onClick={_ => this.setState({ pattern: this.state.pattern.concat(' ') })} />
                    <input type="button" value={'Word end ($)'} onClick={_ => this.setState({ pattern: this.state.pattern.concat('$') })} />
                </div>
            <div>
                {this.state.chars.map(c =>
                    <input type="button" value={c} onClick={_ => this.setState({ pattern: this.state.pattern.concat(c) })} />
                )}
            </div>
            <div><textarea style={{ width: "100%" }} value={this.state.pattern} onChange={ev => this.setState({pattern: ev.target.value})} />
            </div>
                <div><input type="button" onClick={this.find.bind(this)} value="Find" />
                </div>
                <div id="result">{
                    data.map(([k, v]) =>
                        <div className="line"><span className="first">{k}</span>
                            {v.map(
                                transc => <span className={"transc " + (transc[0] == 'a' ? 'american' : 'generic')}>
                                    {transc.slice(1).map(part =>
                                        <span className={part[0]}>{part[1]}</span>
                                    )}
                                    </span>
                            )}
                        </div>)
                }

                </div>
           </div>
    );
  }

  render() {
    let contents = this.state.loading
      ? <p><em>Loading...</em></p>
        : this.state.error
            ? <p><em>{this.state.error.name + ' ' + this.state.error.message}</em></p>
            :this.renderTable(this.state);

    return (
      <div>
        {contents}
      </div>
    );
  }

  async fetchChars() {
    const response = await fetch('dict');
    const data = await response.json();
    this.setState({ chars: data, loading: false });
  }
}
